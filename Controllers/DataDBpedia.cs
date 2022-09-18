using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Text.Encodings.Web;
using VDS.RDF.Query;
using VDS.RDF.Shacl.Validation;

namespace APIs.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class DataDBpedia : ControllerBase
    {
        [HttpGet("{endpoint}/{entity}")]
        public IActionResult DBpediaTriples(string endpoint, string entity)
        {
            //string endpoint = "https://dbpedia.org/sparql";
            //string entity = "http://dbpedia.org/resource/Socrates";

            endpoint = endpoint.Replace("%2F", "/");
            entity = entity.Replace("%2F", "/");

            Boolean bothSides = false;
            string triples = getJSONforSPARQLQuery(entity, endpoint, bothSides);

            //JObject json = JObject.Parse(triples);
            return Ok(triples);
            //return new JsonResult(triples);
        }

        [HttpGet("images/{endpoint}/{entity}")]
        public IActionResult DBpediaImages(string endpoint, string entity)
        {
            endpoint = endpoint.Replace("%2F", "/");
            entity = entity.Replace("%2F", "/");
            string images = getAllImages(entity, endpoint);

            return Ok(images);
        }

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

        private string getJSONforSPARQLQuery(string URLofEntity, string endpoint1, bool bothSides)
        {
            string jsonOutput = "[";
            string query = "Select ?predicate ?object where {<" + URLofEntity + "> ?predicate ?object}";
            if (bothSides == true)
            {
                query = "Select ?predicate ?object where {{<" + URLofEntity + "> ?predicate ?object} UNION {?object ?predicate <" + URLofEntity + ">}}";
            }
            try
            {
                SparqlRemoteEndpoint endpoint = new SparqlRemoteEndpoint(new Uri(endpoint1));
                SparqlResultSet rset = endpoint.QueryWithResultSet(query);

                foreach (SparqlResult result in rset.Results)
                {
                    string predicate = result.Value("predicate").ToString();
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
    }
}