// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Knowledge.Services.Models
{
    public class QueryParameters
    {
        public string ScoringProfile { get; set; }

        public int RowCount { get; set;  }

        public List<string> inOrderBy { get; set;  }

        public QueryParameters()
        {
            this.RowCount = 10;
            this.inOrderBy = new List<string>();
        }
    }
}
