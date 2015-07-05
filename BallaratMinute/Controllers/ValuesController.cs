using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Data.Entity.Spatial;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Xml.Linq;
using BallaratMinute.Models;
using CsQuery;
using CsvHelper;
using CsvHelper.Configuration;
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
            try
            {
                list = GetColesLocations(latitude, longitude, list);
            }
            catch { }
            return list;
        }

        private List<VicPoint> GetColesLocations(double latitude, double longitude, List<VicPoint> list)
        {
            string csv;
            using (WebClient client = new WebClient())
            {
                csv =
                    client.DownloadString(
                        "https://www.coles.com.au/store-locator/search?lon="+ longitude + "&lat="+ latitude + "&kms=3");
            }
            string line;
            string javascript = "";
            using (TextReader file = new StringReader(csv))
            {
                while ((line = file.ReadLine()) != null)
                {
                    if (!line.Contains("var storeMapInfoList = [")) continue;
                    javascript = line;
                    break;
                }
            }
            if (!string.IsNullOrWhiteSpace(javascript))
            {
                javascript = javascript.Replace("var storeMapInfoList = ", "").Replace(";","");
                List<ColesLocation> locations = JsonConvert.DeserializeObject<List<ColesLocation>>(javascript);
                var queriedLocation = DbGeography.FromText("POINT(" + longitude + " " + latitude + ")");

                foreach (var location in locations)
                {
                    var thisLocation = DbGeography.FromText("POINT(" + location.Longitude + " " + location.Latitude + ")");
                    var vp = new VicPoint
                    {
                        address = "",
                        type = 0,
                        typeName = "Coles",
                        name = location.Title,
                        biketime = (int) ((thisLocation.Distance(queriedLocation).Value/5)/60),
                        walktime = (int) ((thisLocation.Distance(queriedLocation).Value/1.4)/60),
                        coords = new VicPoint.Coords()
                        {
                            lat = Double.Parse(location.Latitude),
                            @long = Double.Parse(location.Longitude)
                        },
                        desc = location.Address,
                        id = 0,
                        distance = thisLocation.Distance(queriedLocation)
                    };
                    list.Add(vp);
                }

            }

            return list;
        }

        public class ColesLocation
        {
                public string BrandId { get; set; }
                public string Id { get; set; }
                public string Title { get; set; }
                public string Address { get; set; }
                public string Phone { get; set; }
                public List<string> Hours { get; set; }
                public string Longitude { get; set; }
                public string Latitude { get; set; }
                public string StoreLink { get; set; }
                public List<object> OpeningHoursExceptions { get; set; }
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
            //EducationFacilities();
            //Toilets();
            //LicensedVenues();
            //BusShelters();
            //Childcare();
            //Hospitals();
            Centrelink();
        }

        private static void Centrelink()
        {
            
                
            string csv;
            using (WebClient client = new WebClient())
            {
                csv =
                    client.DownloadString(
                        "http://data.gov.au/dataset/70c2b2fe-2a32-450e-98dc-453fe4a02aae/resource/5a45d7b2-8579-425b-bb46-53a0e0bfa053/download/Centrelink-Office-Locations-as-at-4-June-2015.csv");
            }
            using (TextReader sr = new StringReader(csv))
            {
                BallaratMinuteModel model = new BallaratMinuteModel();
                //List<string> features = m.features.Select(feature => feature.properties.PlayType).ToList().Distinct().OrderBy(i => i).ToList();

                model.POITypes.AddOrUpdate(p => p.Name, new POIType() { Name = "Centrelink" });
                model.SaveChanges();

                var csvFile = new CsvReader(sr);
                while (csvFile.Read())
                {
                    model.PointsOfInterest.AddOrUpdate(i => i.Name, new PointOfInterest()
                    {
                        TypeID = model.POITypes.Single(j => j.Name == "Centrelink").ID,
                        Coordinates =
                            DbGeography.FromText("POINT(" + csvFile.GetField<string>("LONGITUDE") + " " +
                                                    csvFile.GetField<string>("LATITUDE") + ")"),
                        Desc = csvFile.GetField<string>("ADDRESS") + ", " + csvFile.GetField<string>("SUBURB") + " " + csvFile.GetField<string>("POSTCODE"),
                        Name = csvFile.GetField<string>("OFFICE TYPE") + " - " + csvFile.GetField<string>("SITE NAME")
                    });
                    model.SaveChanges();
                }
            }
        }
        
        private void Hospitals()
        {
            XDocument xdoc = XDocument.Load(@"doc.kml");
            BallaratMinuteModel model = new BallaratMinuteModel();
            model.POITypes.AddOrUpdate(p => p.Name, new POIType() { Name = "Hospital" });
            model.SaveChanges();

            var query = xdoc.Root
               .Element("Document")
               .Elements("Placemark")
               .Select(x => new  // I assume you've already got this
               {
                   Name = x.Element("name").Value,
                   Description = x.Element("description").Value,
                   // etc
                   Coords = x.Element("Point").Element("coordinates").Value
               });

            foreach (var placeMark in query)
            {
                CQ desc = placeMark.Description;

                var test = desc["td"];
                var t2 = test.Single(i => i.InnerText == "Address");
                var addr = t2.NextSibling.InnerText;

                var t3 = test.Single(i => i.InnerText == "Town");
                var town = t3.NextSibling.InnerText;

                var t4 = test.Single(i => i.InnerText == "Postcode");
                var postcode = t4.NextSibling.InnerText;

               var address = addr + ", " + town + " " + postcode;

                model.PointsOfInterest.AddOrUpdate(i => i.Name, new PointOfInterest()
                {
                    TypeID = model.POITypes.Single(j => j.Name == "Hospital").ID,
                    Coordinates =
                        DbGeography.FromText("POINT(" + placeMark.Coords.Split(',')[0] + " " +
                                             placeMark.Coords.Split(',')[1] + ")"),
                    Desc = address,
                    Name = placeMark.Name 
                });
                model.SaveChanges();
            }
        }

        private void Childcare()
        {
            string json;
            using (WebClient client = new WebClient())
            {
                json =
                    client.DownloadString(
                        "https://data.gov.au/dataset/c8642f6b-0c28-48d2-867f-4c95ed64a84a/resource/76100de9-9c7d-4c9c-bdf7-a5967fad7863/download/ballaratchildcarecentres.json");
            }

            var m = JsonConvert.DeserializeObject<List<BallaratChildCare>>(json);
            BallaratMinuteModel model = new BallaratMinuteModel();
            //List<string> features = m.features.Select(feature => feature.properties.PlayType).ToList().Distinct().OrderBy(i => i).ToList();

            model.POITypes.AddOrUpdate(p => p.Name, new POIType() { Name = "Childcare Centre" });
            model.SaveChanges();

            foreach (var feature in m)
            {
                model.PointsOfInterest.AddOrUpdate(i => i.Name, new PointOfInterest()
                {
                    TypeID = model.POITypes.Single(j => j.Name == "Childcare Centre").ID,
                    Coordinates =
                        DbGeography.FromText("POINT(" + feature.lon + " " +
                                             feature.lat + ")"),
                    Desc = feature.address,
                    Name = feature.name
                });
                model.SaveChanges();
            }
        }

        private class BallaratChildCare
        {
            public string json_featuretype { get; set; }
            public string ID { get; set; }
            public string name { get; set; }
            public string address { get; set; }
            public string contact_ph1 { get; set; }
            public string contact_ph2 { get; set; }
            public string email { get; set; }
            public string url { get; set; }
            public string lat { get; set; }
            public string lon { get; set; }
            public string json_ogc_wkt_crs { get; set; }
            public JsonGeometry json_geometry { get; set; }
            public class JsonGeometry
            {
                public string type { get; set; }
                public List<double> coordinates { get; set; }
            }
        }

        private void BusShelters()
        {
            //
            string json;
            using (WebClient client = new WebClient())
            {
                json =
                    client.DownloadString(
                        "https://data.gov.au/dataset/bea6519d-2785-4435-9d1e-a7ca070d675d/resource/26bb3915-a9a9-408f-9496-e1671517886a/download/busshelters.json");
            }

            BallaratBusShelter m = JsonConvert.DeserializeObject<BallaratBusShelter>(json);
            BallaratMinuteModel model = new BallaratMinuteModel();
            //List<string> features = m.features.Select(feature => feature.properties.PlayType).ToList().Distinct().OrderBy(i => i).ToList();
            foreach (var feature in m.features)
            {
                model.POITypes.AddOrUpdate(p => p.Name, new POIType() { Name = feature.properties.Feature_Ty });
                model.SaveChanges();

                if (feature.geometry == null) continue;
                model.PointsOfInterest.AddOrUpdate(i => i.Name, new PointOfInterest()
                {
                    TypeID = model.POITypes.Single(j => j.Name == feature.properties.Feature_Ty).ID,
                    Coordinates =
                        DbGeography.FromText("POINT(" + feature.geometry.coordinates[0] + " " +
                                             feature.geometry.coordinates[1] + ")"),
                    Desc = feature.properties.Feature_Lo,
                    Name = feature.properties.Site_Name
                });
                model.SaveChanges();
            }
        }

        private class BallaratBusShelter
        {
            

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
                    public string Central_As { get; set; }
                    public string Site_Name { get; set; }
                    public string Feature_Lo { get; set; }
                    public string Feature_Ty { get; set; }
                    public double Centroid_E { get; set; }
                    public double Centroid_N { get; set; }
                }
            }
            
                public string name { get; set; }
                public string type { get; set; }
                public List<Feature> features { get; set; }
        }

        //private static void LicensedVenues()
        //{
        //    //
        //    string csv;
        //    using (WebClient client = new WebClient())
        //    {
        //        csv =
        //            client.DownloadString(
        //                "https://www.data.vic.gov.au/data/dataset/62d969b6-2986-4d8f-927f-1f577be85994/resource/a632e594-8de3-428d-aac4-ab601db1177d/download/.fileslicences.csv");
        //    }
        //    var csvFile = new CsvReader(csv);
        //    var records = csvFile.GetRecords<BallaratLicense>();
        //}

        //private class BallaratLicense
        //{
        //    public int LicenceNo;
        //    public int LicenceType;
        //    public int PremisesName;
        //    public int Licensee;
        //    public int Address1;
        //    public int Address2;
        //    public int Suburb;
        //    public int Postcode;
        //}

        private static void Toilets()
        {
            string json;
            using (WebClient client = new WebClient())
            {
                json =
                    client.DownloadString(
                        "http://data.gov.au/geoserver/ballarat-public-toilets/wfs?request=GetFeature&typeName=4f875c86_2a8c_4daf_b40d_dca04aab49ea&outputFormat=json");
            }

            BallaratToilet m = JsonConvert.DeserializeObject<BallaratToilet>(json);
            BallaratMinuteModel model = new BallaratMinuteModel();
            //List<string> features = m.features.Select(feature => feature.properties.PlayType).ToList().Distinct().OrderBy(i => i).ToList();
            foreach (var feature in m.features)
            {
                model.POITypes.AddOrUpdate(p => p.Name, new POIType() { Name = feature.properties.type });
                model.SaveChanges();

                if (feature.geometry == null) continue;
                model.PointsOfInterest.AddOrUpdate(i => i.Name, new PointOfInterest()
                {
                    TypeID = model.POITypes.Single(j => j.Name == feature.properties.type).ID,
                    Coordinates =
                        DbGeography.FromText("POINT(" + feature.geometry.coordinates[0] + " " +
                                             feature.geometry.coordinates[1] + ")"),
                    Desc = feature.properties.location,
                    Name = feature.properties.site
                });
                model.SaveChanges();
            }
        }
        public class BallaratToilet
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
                    public string council_id { get; set; }
                    public string location { get; set; }
                    public string site { get; set; }
                    public string type { get; set; }
                    public string accessible { get; set; }
                    public string ambulant { get; set; }
                    public string dda_access { get; set; }
                    public string doorsauto { get; set; }
                    public string socialuse { get; set; }
                    public string lat { get; set; }
                    public string @long { get; set; }
                    public object geom { get; set; }
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
