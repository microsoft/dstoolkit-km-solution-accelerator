// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Services.Common
{
    public class IHelpers
    {
        public static string GetImageFileName(string filename)
        {
            string[] tokens = filename.Split('/');

            return tokens[tokens.Length-1];
        }
        public static string GetShortFileName(string filename, bool withEndingDot = true)
        {
            string[] tokens = filename.Split('.');

            StringBuilder tempname = new StringBuilder();

            for (int i = 2; i < tokens.Count(); i++)
            {
                tempname.Append(tokens[i]);

                if (i < (tokens.Count() - 1)) { tempname.Append("."); }
            }

            return tempname.ToString();
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        public static string GetPartitionKeyFromFilename(string filename)
        {
            string[] tokens = filename.Split('/');

            return tokens[1];
        }
        public static string GetRowKeyFromFilename(string filename)
        {
            string[] tokens = filename.Split('/');

            if ( tokens.Length > 2)
            {
                string[] tokens2 = tokens[2].Split(".");

                return tokens2[0].Trim();

            }
            return String.Empty;
        }


        public static string GetDriveFromResource(string resource)
        {
            string[] tokens = resource.Split('/');

            return tokens[1];
        }
        public static string GetTokenFromDeltaUrl(string deltaurl)
        {
            string[] tokens = deltaurl.Split("token='");

            if (tokens.Length > 1 )
            {
                string[] tokens2 = tokens[1].Split("')");

                return tokens2[0].Trim();
            }
            else
            {
                tokens = deltaurl.Split("token=");

                return tokens[1];
            }
        }
    }
    public static class FEnvironment
    {
        static FEnvironment() { }

        static string[] trueStringArray = { "true", "on", "1", "enable", "enabled" };
        public static bool BooleanReader(string key, bool defaultValue)
        {
            string s = System.Environment.GetEnvironmentVariable(key);
            if (String.IsNullOrEmpty(s))
            {
                return defaultValue;
            }

            if (trueStringArray.Any(s.ToLower().Contains))
            {
                return true;
            }

            return false;
        }

        public static int IntegerReader(string key, int defaultValue)
        {
            string s = System.Environment.GetEnvironmentVariable(key);
            if (String.IsNullOrEmpty(s))
            {
                return defaultValue;
            }

            if (int.TryParse(s, out int r))
            {
                return r;
            }

            return defaultValue;

        }
        public static string StringReader(string key, string defaultValue)
        {
            string s = System.Environment.GetEnvironmentVariable(key);
            if (String.IsNullOrEmpty(s))
            {
                return defaultValue;
            }

            return s;
        }
        public static string StringReader(string key)
        {
            string s = System.Environment.GetEnvironmentVariable(key);

            if (String.IsNullOrEmpty(s))
            {
                return String.Empty;
            }

            return s;
        }
        public static List<string> StringArrayReader(string key, char separator = ',')
        {
            string sNone = System.Environment.GetEnvironmentVariable(key);

            if (!String.IsNullOrEmpty(sNone))
            {
                return sNone.Split(separator).ToList<string>();
            }

            return new List<string>();
        }
        public static string[] StringArrayReader(string key, int maximum)
        {
            // looking for KeyName, KeyName0, KeyName1, ... KeyNameMaximum
            int current = 0;
            string[] array = new string[maximum];
            // check for KeyName
            string sNone = System.Environment.GetEnvironmentVariable(key);
            if (!String.IsNullOrEmpty(sNone))
            {
                array[current++] = sNone;
            }

            // make sure we can retrieve KeyName0 through KeyNameMaximum, but not exceed maximum entries
            for (int i = 0; i <= maximum && current < maximum; i++)
            {
                string s = System.Environment.GetEnvironmentVariable(key + i);
                if (!String.IsNullOrEmpty(s))
                {
                    array[current++] = s;
                }
            }

            return array;
        }
        public static int[] IntegerArrayReader(string key, int maximum)
        {
            // looking for KeyName, KeyName0, KeyName1, ... KeyNameMaximum
            int current = 0;
            int[] array = new int[maximum];
            // check for KeyName
            string sNone = System.Environment.GetEnvironmentVariable(key);
            if (!String.IsNullOrEmpty(sNone))
            {
                if (int.TryParse(sNone, out int rNone))
                {
                    array[current++] = rNone;
                }
            }
            // make sure we can retrieve KeyNameMaximum, but not exceed maximum entries
            for (int i = 0; i <= maximum && current < maximum; i++)
            {
                string s = System.Environment.GetEnvironmentVariable(key + i);
                if (String.IsNullOrEmpty(s))
                {
                    if (int.TryParse(s, out int r))
                    {
                        array[current++] = r;
                    }
                }
            }

            return array;
        }
    }
}
