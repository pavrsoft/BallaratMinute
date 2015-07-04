using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Data.Entity.Spatial;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using BallaratMinute.Models;
using Newtonsoft.Json;

namespace BallaratMinute.Controllers
{
    public class ValuesController : ApiController
    {
        // GET api/values
        public IEnumerable<PointOfInterest> Get()
        {
            BallaratMinuteModel model = new BallaratMinuteModel();
            return model.PointsOfInterest;
        }

        // GET api/values/5
        public PointOfInterest Get(long id)
        {
            BallaratMinuteModel model = new BallaratMinuteModel();
            return model.PointsOfInterest.Single(i => i.ID == id);
        }

        public List<VicPoint> GetFromCoordinates(double latitude, double longitude)
        {
            var queriedLocation = DbGeography.FromText("POINT(" + longitude + " " + latitude + ")");

            BallaratMinuteModel model = new BallaratMinuteModel();

            var list = model.PointsOfInterest
                .Where(i => i.Coordinates.Distance(queriedLocation) <= 3000)
                .OrderBy(i=>i.Coordinates.Distance(queriedLocation))
                .Select(i => new VicPoint
            {
                address = i.Address,
                type = i.TypeID,
                typeName = i.Type.Name,
                name = i.Name,
                biketime = (int)((i.Coordinates.Distance(queriedLocation).Value / 5) / 60),
                walktime = (int)((i.Coordinates.Distance(queriedLocation).Value / 1.4)/60),
                coords = new VicPoint.Coords()
                {
                    lat = i.Coordinates.Latitude.Value,
                    @long = i.Coordinates.Longitude.Value
                },
                desc = i.Desc,
                id = i.ID,
                distance = i.Coordinates.Distance(queriedLocation)
            }).ToList();

            //var list2 = model.PointsOfInterest.Select(i => new { dist = i.Coordinates.Distance(queriedLocation) });

            return list;
        }

        public class VicPoint
        {
            public double? distance;
            public long id { get; set; }
            public string name { get; set; }
            public string desc { get; set; }
            public long type { get; set; }
            public string typeName { get; set; }
            public Coords coords { get; set; }
            public string address { get; set; }
            public int walktime { get; set; }
            public int biketime { get; set; }

            public class Coords
            {
                public double lat { get; set; }
                public double @long { get; set; }
            }
        }

        // POST api/values
        public void Post([FromBody]VicPoint value)
        {
            //feature_ty": "PG-Skate Parks",
            BallaratMinuteModel model = new BallaratMinuteModel();
            var poiType = new POIType()
            {
                Name = "B-Child Minding Playground"
            };
            model.POITypes.AddOrUpdate(p => p.Name, poiType);
            model.SaveChanges();
            model.PointsOfInterest.AddOrUpdate(new PointOfInterest()
            {
                Name = "Alfredton Pre School",//Site
                Desc = "Alfredton Pre School Play Equipment\n6-8 Balyarta Street\nAlfredton",
                TypeID = poiType.ID,
                Coordinates = DbGeography.FromText("POINT(143.80650095599 -37.5523078953505)")
            });
            model.SaveChanges();
        }

        // PUT api/values/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        public void Delete(long id)
        {
            BallaratMinuteModel model = new BallaratMinuteModel();
            var poi = model.PointsOfInterest.SingleOrDefault(i => i.ID == id);
            if (poi != null)
                model.PointsOfInterest.Remove(poi);
        }

        public void Import(bool ballarat)
        {
            //PlayGrounds();
            //Kindergartens();
            EducationFacilities();
        }

        private static void EducationFacilities()
        {
            string json;
            using (WebClient client = new WebClient())
            {
                json =
                    client.DownloadString(
                        "http://data.gov.au/geoserver/ballarat-education-facilities/wfs?request=GetFeature&typeName=02dc15c8_cd31_4b2f_abd0_276e59e391c3&outputFormat=json");
            }

            BallaratEducationFacility m = JsonConvert.DeserializeObject<BallaratEducationFacility>(json);
            BallaratMinuteModel model = new BallaratMinuteModel();
            //List<string> features = m.features.Select(feature => feature.properties.PlayType).ToList().Distinct().OrderBy(i => i).ToList();
            foreach (var feature in m.features)
            {
                model.POITypes.AddOrUpdate(p => p.Name, new POIType() { Name = feature.properties.service });
                model.SaveChanges();

                if (feature.geometry == null) continue;
                model.PointsOfInterest.AddOrUpdate(i => i.Name, new PointOfInterest()
                {
                    TypeID = model.POITypes.Single(j => j.Name == feature.properties.service).ID,
                    Coordinates = 
                        DbGeography.FromText("POINT(" + feature.geometry.coordinates[0] + " " +
                                             feature.geometry.coordinates[1] + ")"),
                    Desc = feature.properties.location,
                    Name = feature.properties.name
                });
                model.SaveChanges();
            }
        }
        public class BallaratEducationFacility
        {
            public string type { get; set; }
            public int totalFeatures { get; set; }
            public List<Feature> features { get; set; }
            public Crs crs { get; set; }

            public class Feature
            {
                public string type { get; set; }
                public string id { get; set; }
                public Geometry geometry { get; set; }
                public string geometry_name { get; set; }
                public Properties properties { get; set; }
                public class Geometry
                {
                    public string type { get; set; }
                    public List<double> coordinates { get; set; }
                }
                public class Properties
                {
                    public string councilid { get; set; }
                    public string name { get; set; }
                    public string location { get; set; }
                    public string service { get; set; }
                    public string lat { get; set; }
                    public string @long { get; set; }
                }
            }
            public class Crs
            {
                public string type { get; set; }
                public Properties2 properties { get; set; }
                public class Properties2
                {
                    public string name { get; set; }
                }
            }
        }
        private static void Kindergartens()
        {
            string json;
            using (WebClient client = new WebClient())
            {
                json =
                    client.DownloadString(
                        "http://data.gov.au/geoserver/ballarat-kindergartens/wfs?request=GetFeature&typeName=7d2e9e5e_f653_487f_a272_1c54d7a37e47&outputFormat=json");
            }

            BallaratKindergarten m = JsonConvert.DeserializeObject<BallaratKindergarten>(json);
            BallaratMinuteModel model = new BallaratMinuteModel();
            //List<string> features = m.features.Select(feature => feature.properties.PlayType).ToList().Distinct().OrderBy(i => i).ToList();
            model.POITypes.AddOrUpdate(p => p.Name, new POIType() { Name = "Kindergarten" });
            model.SaveChanges();
            foreach (var feature in m.features)
            {
                

                if (feature.geometry == null) continue;
                model.PointsOfInterest.AddOrUpdate(i => i.Name, new PointOfInterest()
                {
                    TypeID = model.POITypes.Single(j => j.Name == "Kindergarten").ID,
                    Coordinates =
                        DbGeography.FromText("POINT(" + feature.geometry.coordinates[0] + " " +
                                             feature.geometry.coordinates[1] + ")"),
                    Desc = feature.properties.address,
                    Name = feature.properties.name
                });
                model.SaveChanges();
            }
        }
        public class BallaratKindergarten
        {
            public string type { get; set; }
            public int totalFeatures { get; set; }
            public List<Feature> features { get; set; }
            public Crs crs { get; set; }

            public class Geometry
            {
                public string type { get; set; }
                public List<double> coordinates { get; set; }
            }



            public class Feature
            {
                public string type { get; set; }
                public string id { get; set; }
                public Geometry geometry { get; set; }
                public string geometry_name { get; set; }
                public Properties properties { get; set; }

                public class Properties
                {
                    public int councilid { get; set; }
                    public string name { get; set; }
                    public string address { get; set; }
                    public double phone { get; set; }
                    public string email { get; set; }
                    public string enrolment { get; set; }
                    public string info { get; set; }
                    public string lat { get; set; }
                    public string @long { get; set; }
                }
            }



            public class Crs
            {
                public string type { get; set; }
                public Properties2 properties { get; set; }

                public class Properties2
                {
                    public string name { get; set; }
                }
            }
        }
        private static void PlayGrounds()
        {
            string json;
            using (WebClient client = new WebClient())
            {
                json =
                    client.DownloadString(
                        "http://data.gov.au/dataset/a9b248c1-2078-45fa-b9c6-b2ae562c87b2/resource/693b8663-efd6-4583-9dd6-7a3793e54bae/download/BallaratPlaygrounds.geojson");
            }

            BallaratPlayground m = JsonConvert.DeserializeObject<BallaratPlayground>(json);
            BallaratMinuteModel model = new BallaratMinuteModel();
            //List<string> features = m.features.Select(feature => feature.properties.PlayType).ToList().Distinct().OrderBy(i => i).ToList();
            foreach (var feature in m.features)
            {
                model.POITypes.AddOrUpdate(p => p.Name, new POIType() {Name = feature.properties.PlayType});
                model.SaveChanges();

                if (feature.geometry == null) continue;
                model.PointsOfInterest.AddOrUpdate(i => i.Name, new PointOfInterest()
                {
                    TypeID = model.POITypes.Single(j => j.Name == feature.properties.PlayType).ID,
                    Coordinates =
                        DbGeography.FromText("POINT(" + feature.geometry.coordinates[0] + " " +
                                             feature.geometry.coordinates[1] + ")"),
                    Desc = feature.properties.Location,
                    Name = feature.properties.Site
                });
                model.SaveChanges();
            }
        }

        public class BallaratPlayground
        {
            public string name { get; set; }
            public string type { get; set; }
            public List<Feature> features { get; set; }
            public class Feature
            {
                public string type { get; set; }
                public Geometry geometry { get; set; }
                public Properties properties { get; set; }
                public class Geometry
                {
                    public string type { get; set; }
                    public List<double> coordinates { get; set; }
                }

                public class Properties
                {
                    public string Area { get; set; }
                    public string ID { get; set; }
                    public string Location { get; set; }
                    public string PlayType { get; set; }
                    public string Maintain { get; set; }
                    public string Site { get; set; }
                    public string Ward { get; set; }
                    public string Constructed { get; set; }
                }
            }
        }
    }
}
