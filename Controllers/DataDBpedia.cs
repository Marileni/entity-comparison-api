using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Text.Encodings.Web;
using VDS.RDF.Query;
using VDS.RDF.Shacl.Validation;
using static System.Net.WebRequestMethods;

namespace APIs.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class DataDBpedia : ControllerBase
    {

        //Get all triples
        [HttpGet("{entity}")]
        public IActionResult DBpediaTriples(string entity)
        {
            string endpoint = "http://dbpedia.org/sparql";
            entity = entity.Replace("%2F", "/");

            Boolean bothSides = true;
            string triples = getJSONforSPARQLQuery(entity, endpoint, bothSides);

            return Ok(triples);
        }

        //Get all common of two entities
        [HttpGet("common/entity/{entity1}/{entity2}")]
        public IActionResult DBpediaTriplesCommon(string entity1, string entity2)
        {
            string endpoint = "http://dbpedia.org/sparql";
            entity1 = entity1.Replace("%2F", "/");
            entity2 = entity2.Replace("%2F", "/");

            Boolean bothSides = true;
            string triples = getJSONforSPARQLQueryCommon(entity1, entity2, endpoint, bothSides);

            return Ok(triples);
        }

        //Get all properties of the entity
        [HttpGet("all/properties/of/entity/{entity1}")]
        public IActionResult DBpediaTriplesAllProp(string entity1)
        {
            string endpoint = "http://dbpedia.org/sparql";
            entity1 = entity1.Replace("%2F", "/");

            Boolean bothSides = true;
            string triples = getAllProperties(entity1, endpoint, bothSides);

            return Ok(triples);
        }

        //Get the specific property
        [HttpGet("specific/{property}/{entity1}")]
        public IActionResult DBpediaTriplesSpecificPr(string entity1, string property)
        {
            string endpoint = "http://dbpedia.org/sparql";
            entity1 = entity1.Replace("%2F", "/");
            property = property.Replace("%2F", "/");

            Boolean bothSides = false;
            string triples = getSpecificProperty(entity1, property, endpoint, bothSides);

            return Ok(triples);
        }

        //Get all images
        [HttpGet("images/{entity}")]
        public IActionResult DBpediaImages(string entity)
        {
            string endpoint = "http://dbpedia.org/sparql";
            entity = entity.Replace("%2F", "/");
            string images = getAllImages(entity, endpoint);

            return Ok(images);
        }

        //get All Properties of an entity
        private string getAllProperties(string URLofEntity, string endpoint1, bool bothSides)
        {
            string jsonOutput = "[";
            jsonOutput += "{\"predicate\":\"http://www.w3.org/1999/02/22-rdf-syntax-ns#type\"},\n";
            string query = "Select distinct ?predicate where {<" + URLofEntity + "> ?predicate ?object} order by asc(str(?predicate))";
            List<string> allPredicates = new List<string>();
            try
            {
                SparqlRemoteEndpoint endpoint = new SparqlRemoteEndpoint(new Uri(endpoint1));
                SparqlResultSet rset = endpoint.QueryWithResultSet(query);

                Console.WriteLine(rset.Results);

                foreach (SparqlResult result in rset.Results)
                {
                    string predicate = result.Value("predicate").ToString();

                    if (predicate != "http://www.w3.org/1999/02/22-rdf-syntax-ns#type")
                        allPredicates.Add(predicate);

                    //jsonOutput += "{\"predicate\":\"" + predicate.Replace("http://dbpedia.org/property/", "dbp:").Replace("http://dbpedia.org/ontology/", "dbo:") + "\"},\n";
                }

                if (bothSides == true)
                {
                    string query2 = "Select distinct ?predicate where { ?object ?predicate <" + URLofEntity + "> } order by asc(str(?predicate))";
                    SparqlRemoteEndpoint endpoint2 = new SparqlRemoteEndpoint(new Uri(endpoint1));
                    SparqlResultSet rset2 = endpoint2.QueryWithResultSet(query2);


                    foreach (SparqlResult result2 in rset2.Results)
                    {
                        string predicate = result2.Value("predicate").ToString() + "*";
                        allPredicates.Add(predicate);
                        //jsonOutput += "{\"predicate\":\"" +predicate.Replace("http://dbpedia.org/property/", "dbp:").Replace("http://dbpedia.org/ontology/", "dbo:") + "*\"},\n";
                    }
                }
                allPredicates.Sort();
                foreach (var pred in allPredicates.Distinct())
                    jsonOutput += "{\"predicate\":\"" + pred.Replace("http://dbpedia.org/property/", "dbp:").Replace("http://dbpedia.org/ontology/", "dbo:") + "\"},\n";
                jsonOutput = jsonOutput.Remove(jsonOutput.Length - 2);
                jsonOutput += "]";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
                return "Error";
            }

            return jsonOutput;
        }

        //Get the data of a specific property
        private string getSpecificProperty(string URLofEntity, string property, string endpoint1, bool bothSides)
        {
            String newProperty = property.Replace("dbp:", "http://dbpedia.org/property/").Replace("dbo:", "http://dbpedia.org/ontology/");
            string query = "Select <" + newProperty + "> as ?predicate ?object where {<" + URLofEntity + "> <" + newProperty + "> ?object}";
            string jsonOutput = "[";
            if (property.EndsWith("*"))
            {
                string property2 = newProperty.Remove(newProperty.Length - 1);
                query = "Select <" + property2 + "> as ?predicate ?object where {?object <" + property2 + "> <" + URLofEntity + ">}";
            }

            try
            {
                SparqlRemoteEndpoint endpoint = new SparqlRemoteEndpoint(new Uri(endpoint1));
                SparqlResultSet rset = endpoint.QueryWithResultSet(query);

                foreach (SparqlResult result in rset.Results)
                {
                    string predicate = property;//result.Value("predicate").ToString();
                    string objectSP = result.Value("object").ToString();

                    objectSP = objectSP.Replace("\"", "");
                    objectSP = objectSP.Replace("\\", "\\\\");
                    objectSP = objectSP.Replace("\n", " ");

                    jsonOutput += "{\"predicate\":\"" + predicate + "\",\"object\":\"" + objectSP + "\"},\n";
                }
                jsonOutput = jsonOutput.Remove(jsonOutput.Length - 2);
                jsonOutput += "]";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
                return "Error";
            }

            return jsonOutput;
        }


        //Get the data of the entity
        private string getJSONforSPARQLQuery(string URLofEntity, string endpoint1, bool bothSides)
        {
            string jsonOutput = "[";
            string query = "Select ?predicate ?object where {<" + URLofEntity + "> ?predicate ?object}";

            try
            {
                SparqlRemoteEndpoint endpoint = new SparqlRemoteEndpoint(new Uri(endpoint1));
                SparqlResultSet rset = endpoint.QueryWithResultSet(query);

                foreach (SparqlResult result in rset.Results)
                {
                    string predicate = result.Value("predicate").ToString();
                    if (predicate.StartsWith("http://dbpedia.org/ontology/wiki") || predicate.StartsWith("http://dbpedia.org/property/wiki"))
                    {
                        continue;
                    }
                    string objectSP = result.Value("object").ToString();

                    objectSP = objectSP.Replace("\"", "");
                    objectSP = objectSP.Replace("\\", "\\\\");
                    objectSP = objectSP.Replace("\n", " ");

                    jsonOutput += "{\"predicate\":\"" + predicate.Replace("http://dbpedia.org/property/", "dbp:").Replace("http://dbpedia.org/ontology/", "dbo:") + "\",\"object\":\"" + objectSP + "\"},\n";
                }
                if (bothSides == true)
                {
                    string query2 = "Select ?predicate ?object where {?object ?predicate <" + URLofEntity + ">}";

                    SparqlRemoteEndpoint endpoint2 = new SparqlRemoteEndpoint(new Uri(endpoint1));
                    SparqlResultSet rset2 = endpoint.QueryWithResultSet(query2);

                    foreach (SparqlResult result2 in rset2.Results)
                    {
                        string predicate = result2.Value("predicate").ToString();
                        if (predicate.StartsWith("http://dbpedia.org/ontology/wiki") || predicate.StartsWith("http://dbpedia.org/property/wiki"))
                        {
                            continue;
                        }
                        string objectSP = result2.Value("object").ToString();

                        objectSP = objectSP.Replace("\"", "");
                        objectSP = objectSP.Replace("\\", "\\\\");
                        objectSP = objectSP.Replace("\n", " ");

                        jsonOutput += "{\"predicate\":\"" + predicate.Replace("http://dbpedia.org/property/", "dbp:").Replace("http://dbpedia.org/ontology/", "dbo:") + " *\",\"object\":\"" + objectSP + "\"},\n";
                    }

                }

                jsonOutput = jsonOutput.Remove(jsonOutput.Length - 2);
                jsonOutput += "]";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
                return "Error";
            }

            return jsonOutput;
        }

        //Get the common of two entities
        private string getJSONforSPARQLQueryCommon(string URLofEntity1, string URLofEntity2, string endpoint1, bool bothSides)
        {
            string jsonOutput = "[";
            string query = "select * where { <" + URLofEntity1 + "> ?predicate ?object . <" + URLofEntity2 + "> ?predicate ?object. }";

            try
            {
                SparqlRemoteEndpoint endpoint = new SparqlRemoteEndpoint(new Uri(endpoint1));
                SparqlResultSet rset = endpoint.QueryWithResultSet(query);

                foreach (SparqlResult result in rset.Results)
                {
                    Console.WriteLine(result);
                    string predicate = result.Value("predicate").ToString();
                    if (predicate.StartsWith("http://dbpedia.org/ontology/wiki") || predicate.StartsWith("http://dbpedia.org/property/wiki"))
                    {
                        continue;
                    }
                    string objectSP = result.Value("object").ToString();

                    objectSP = objectSP.Replace("\"", "");
                    objectSP = objectSP.Replace("\\", "\\\\");
                    objectSP = objectSP.Replace("\n", " ");

                    jsonOutput += "{\"predicate\":\"" + predicate.Replace("http://dbpedia.org/property/", "dbp:").Replace("http://dbpedia.org/ontology/", "dbo:") + "\",\"object\":\"" + objectSP + "\"},\n";
                }
                if (bothSides == true)
                {
                    string query2 = "Select ?predicate ?object where {{?object ?predicate <" + URLofEntity1 + "> . ?object ?predicate <" + URLofEntity2 + ">}}";
                    SparqlRemoteEndpoint endpoint2 = new SparqlRemoteEndpoint(new Uri(endpoint1));
                    SparqlResultSet rset2 = endpoint2.QueryWithResultSet(query2);

                    foreach (SparqlResult result2 in rset2.Results)
                    {

                        string predicate = result2.Value("predicate").ToString();
                        if (predicate.StartsWith("http://dbpedia.org/ontology/wiki") || predicate.StartsWith("http://dbpedia.org/property/wiki"))
                        {
                            continue;
                        }
                        string objectSP = result2.Value("object").ToString();

                        objectSP = objectSP.Replace("\"", "");
                        objectSP = objectSP.Replace("\\", "\\\\");
                        objectSP = objectSP.Replace("\n", " ");

                        jsonOutput += "{\"predicate\":\"" + predicate.Replace("http://dbpedia.org/property/", "dbp:").Replace("http://dbpedia.org/ontology/", "dbo:") + " *\",\"object\":\"" + objectSP + "\"},\n";
                    }
                }

                jsonOutput = jsonOutput.Remove(jsonOutput.Length - 2);
                jsonOutput += "]";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
                return "Error";
            }

            return jsonOutput;
        }

        //Get all the images
        private string getAllImages(string URLofEntity, string endpoint1)
        {
            String output = "{\"images\":[";
            String query = "Select ?o where{<" + URLofEntity + "> <http://xmlns.com/foaf/0.1/depiction> ?o}";

            try
            {
                SparqlRemoteEndpoint endpoint = new SparqlRemoteEndpoint(new Uri(endpoint1));
                SparqlResultSet rset = endpoint.QueryWithResultSet(query);

                foreach (SparqlResult result in rset.Results)
                {
                    string linkImage = result.Value("o").ToString();
                    output += "\"" + linkImage + "\",\n";
                }
                output = output.Remove(output.Length - 2);
                output += "]}";

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
                return "Error";
            }

            return output;
        }
    }
}
