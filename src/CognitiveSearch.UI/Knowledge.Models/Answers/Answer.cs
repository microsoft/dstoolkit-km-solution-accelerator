// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Models.Answers
{
    public class Answer: IResultItem 
    {
        public string[] questions { get; set; }
        public string answer { get; set; }
        public float score { get; set; }
        public int id { get; set; }
        public string source { get; set; }
        public object[] metadata { get; set; }
        public Context context { get; set; }
    }

    public class Context
    {
        public bool isContextOnly { get; set; }
        public object[] prompts { get; set; }
    }
}
