﻿using MathNet.Numerics.LinearAlgebra;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.IO;

namespace Tarea2.Classes
{
    public class RenderData
    {
        public Vector<double> CameraPosition { get; set; }
        public Dictionary<string, Vector<double>> PointsToRender { get; set; }
        public Vector<double> UDirectorVector { get; set; }
        public Vector<double> WDirectionVector { get; set; }
        public Vector<double> VDirectionVector { get; set; }
    }

    public class VectorJsonConverter : JsonConverter<Vector<double>>
    {
        public override Vector<double> ReadJson(JsonReader reader, Type objectType, Vector<double> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var array = serializer.Deserialize<double[]>(reader);
            return Vector<double>.Build.DenseOfArray(array);
        }

        // Ocupo esto si no se queja por la herencia
        public override void WriteJson(JsonWriter writer, Vector<double> value, JsonSerializer serializer)
        {
           
        }
    }

    public static class LoatDataFromJson
    {
        public static RenderData LoadRenderDataFromJson(string filePath)
        {
            string json = File.ReadAllText(filePath);

            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new VectorJsonConverter());
            RenderData data = JsonConvert.DeserializeObject<RenderData>(json, settings);
            return data;
        }
    }
    
}