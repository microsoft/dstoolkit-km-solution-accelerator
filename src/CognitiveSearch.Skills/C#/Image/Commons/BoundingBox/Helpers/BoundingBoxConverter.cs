// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Image.Commons.BoundingBox.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Image.Commons.BoundingBox.Helpers
{
    public class BoundingBoxConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return false;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return string.Empty;
            }
            else if (reader.TokenType == JsonToken.String)
            {
                return serializer.Deserialize(reader, objectType);
            }
            else
            {
                JArray array = JArray.Load(reader);

                if (array.Count > 4)
                {
                    // The BoundingBox is a list of coordinates in specific order

                    List<Point> points = new List<Point>();
                    int x = array[0].Value<int>();
                    int y = array[1].Value<int>();
                    points.Add(new Point(x, y));
                    x = array[2].Value<int>();
                    y = array[3].Value<int>();
                    points.Add(new Point(x, y));
                    x = array[4].Value<int>();
                    y = array[5].Value<int>();
                    points.Add(new Point(x, y));
                    x = array[6].Value<int>();
                    y = array[7].Value<int>();
                    points.Add(new Point(x, y));

                    return points;

                }
                else
                {
                    var points = array.ToObject<IList<Point>>();

                    return points;

                }

                //JObject obj = JObject.Load(reader);
                //if (obj["Code"] != null)
                //    return obj["Code"].ToString();
                //else
                //    return serializer.Deserialize(reader, objectType);
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
