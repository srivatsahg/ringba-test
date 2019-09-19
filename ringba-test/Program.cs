using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;

namespace ringba_test
{
    class Program
    {
        /// <summary>
        /// How many of each letter are in the file
        /// How many letters are capitalized in the file
        /// The most common word and the number of times it has been seen.
        /// The most common 2 character prefix and the number of occurrences in the text file.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            
            //1. Get the file contents from the URL
            string url = @"https://ringba-test-html.s3-us-west-1.amazonaws.com/TestQuestions/output.txt";
            var queryUrlTask = MakeAsyncRequest(url);
            var response = queryUrlTask.Result;
            var statistics = new WordStatistics();

            //2. Split the words based on Camelcase
            response = SplitWordsBasedOnCamelCase(response);

            //3. Count the number of letters in the response
            statistics.LetterCount = CalculateLetterCount(response); 

            //4. Count the number of occurences where the letter is capitalized
            statistics.CapitalizedLetterCount = CalculateCapitalizedLetterCount(response);

            //5. Count the duplicates and occurences.
            CalculateDuplicateWords(response,statistics);

            //6. Most common prefix 2 character and occurence
            //statistics.CommonPrefix = CommonPrefix(response);


            Console.WriteLine("----------------------------------------------------------------");
            Console.WriteLine("File Content Statistics");
            Console.WriteLine("----------------------------------------------------------------");
            Console.WriteLine(String.Format("Total Letters              = {0,10}", statistics.LetterCount));
            Console.WriteLine(String.Format("Letters in Uppercase       = {0,10}", statistics.CapitalizedLetterCount));
            Console.WriteLine(String.Format("Duplicate words            = {0,10}", statistics.DuplicateWord));
            Console.WriteLine(String.Format("Duplicate word count       = {0,10}", statistics.DuplicateWordCount));
            //Console.WriteLine(String.Format("Smallest prefix            = {0,10}", statistics.CommonPrefix));
            Console.WriteLine("----------------------------------------------------------------");

            Console.WriteLine("End of Program");
            
        }

        private static int CalculateLetterCount(string response)
        {
            return response.ToCharArray().Count();
        }

        private static int CalculateCapitalizedLetterCount(string response)
        {
            return response.Count(c => char.IsUpper(c));
        }

        /// <summary>
        /// Calculates the Duplicate Word occurence in a string response and the count 
        /// </summary>
        /// <param name="response"></param>
        private static void CalculateDuplicateWords(string response, WordStatistics statistics)
        {
            var keyValuePairs = response.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries).GroupBy(word => word)
                              .ToDictionary(kvp => kvp.Key,
                                              kvp => kvp.Count());
            statistics.DuplicateWord = keyValuePairs.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
            statistics.DuplicateWordCount = keyValuePairs[statistics.DuplicateWord];
        }


        /// <summary>
        /// Places a GET request to request the file resource hosted on the S3 bucket from AWS
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static Task<string> MakeAsyncRequest(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.ContentType = "text/plain";
            request.Method = "GET";
            request.Proxy = null;

            Task<WebResponse> task = Task.Factory.FromAsync(
                request.BeginGetResponse,
                asyncResult => request.EndGetResponse(asyncResult),
                (object)null);

            return task.ContinueWith(t => ReadStreamFromResponse(t.Result));
        }

        /// <summary>
        /// Read response from the resource
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        private static string ReadStreamFromResponse(WebResponse result)
        {
            string results = "N/A";

            try
            {
                using (StreamReader sr = new StreamReader(result.GetResponseStream()))
                {
                    results = sr.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                results = ex.Message;
            }
            return results;
        }

        /// <summary>
        /// Splits the response stream based on the Camelcasing
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SplitWordsBasedOnCamelCase(string value)
        {
            if (value.Length > 0)
            {
                var result = new List<char>();
                char[] array = value.ToCharArray();
                foreach (var item in array)
                {
                    if (char.IsUpper(item) && result.Count > 0)
                    {
                        result.Add(' ');
                    }
                    result.Add(item);
                }

                return new string(result.ToArray());
            }
            return value;
        }

        /// <summary>
        /// Calculates the Common prefix
        /// TODO: Incomplete, not yielding desired results
        /// </summary>
        /// <param name="inputResponse"></param>
        /// <param name="commonPrefixList"></param>
        /// <returns></returns>
        public static string CommonPrefix(string inputResponse)
        {
            if (inputResponse == "" || inputResponse.Length < 0)
            {
                return "N/A";
            }
            else
            {
                var stringArrayResponse = inputResponse.Split(' ');
                //var twoLetterPrefix = stringArrayResponse[0].Substring(0, 2);  //Let the prefix be 2 characters of the first string 
                var twoLetterPrefix = stringArrayResponse[0];  //Let the prefix be 2 characters of the first string 
                Dictionary<String, int> kvpCount = new Dictionary<string, int>();

                for (int i = 1; i < stringArrayResponse.Length; i++)
                {
                    int j = 0;
                    string currentString = stringArrayResponse[i];

                    while (j < twoLetterPrefix.Length &&
                                j < currentString.Length &&
                                    twoLetterPrefix.ToCharArray()[j] == currentString.ToCharArray()[j])
                    {
                        j++;
                    }

                    if (j == 0)
                    {
                        return "N/A";
                    }

                    twoLetterPrefix = twoLetterPrefix.Substring(0, j);
                }

                return twoLetterPrefix;
            }
        }
    }



    /// <summary>
    /// Represents snapshot of statistics 
    /// </summary>
    public class WordStatistics
    {
        public int LetterCount { get; set; }

        public int CapitalizedLetterCount { get; set; }

        public string DuplicateWord { get; set; }

        public int DuplicateWordCount { get; set; }

        public string CommonPrefix { get; set; }

    }
}
