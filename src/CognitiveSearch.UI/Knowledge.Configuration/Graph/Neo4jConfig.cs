// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Configuration.Graph
{
    public class Neo4jConfig : AbstractServiceConfig
    {
        public string ConnectionString { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

    }
}
