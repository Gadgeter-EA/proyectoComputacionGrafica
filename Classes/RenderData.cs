using MathNet.Numerics.LinearAlgebra;
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
        
        public double f;
        public double w;
        public double h;
        public double l;
        public double c;

        public double xPMin;
        public double xPMax;
        public double yPMin;
        public double yPmax;

        public Vector<double> FrontNormalVector { get; set; }
        public Vector<double> BackNormalVector { get; set; }
        public Vector<double> LeftNormalVector { get; set; }
        public Vector<double> RightNormalVector { get; set; }
        public Vector<double> TopNormalVector { get; set; }
        public Vector<double> BottomNormalVector { get; set; }

        public double kAmbient;
        public double kDiffuse;
        public double kSpecular;
        public double sForSpecular;

        public Vector<double> lightOrigin { get; set; }
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
