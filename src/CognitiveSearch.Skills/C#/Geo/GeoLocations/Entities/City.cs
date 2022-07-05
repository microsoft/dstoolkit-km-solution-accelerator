// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Geo.Skills.Entities
{
    public class City
    {
        public int id { get; set; }
        public string city { get; set; }
        public string country { get; set; }
        public string capital { get; set; }

        public bool ambiguous { get; set; }
    }
}