using Elasticsearch.Net;
using Elasticsearch.Net.Connection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Configuration;

namespace SourceFilesElasticCrawler
{
    public static class Program
    {
        private const int ERROR_INVALID_APP_CONFIG = 0x667;
        private const int ERROR_DATABASE_ERROR = 0xA0;

        private static ElasticsearchClient ElasticClient; // elastic REST client
        private static long ID = 0;

        public static void Main(string[] args)
        {
            Console.WriteLine(" *** File Importer ***" );

            var appSettings = ConfigurationManager.AppSettings;

            // validate app setting keys are OK
            if((appSettings ["ServerAddress"] == null)
                || (appSettings["ServerPort"] == null))
                
            {
                Console.WriteLine("ERR: config file keys are missing" + ID);
                // in batch file you can write: if errorlevel 160 goto InvAppCfg
                Environment.ExitCode = ERROR_INVALID_APP_CONFIG;
                return;
            }


            // delete database, recreate index
            if (!InitElastic())
            {
                Environment.ExitCode = ERROR_DATABASE_ERROR; 
                return;
            }

            // create an array with file search pattern
            string[] stringSeparators = new string[] { "|" };

            // start with root

            string[] patterns;
            for (int i = 1; appSettings["RootDir" + i.ToString()] != null; i++)
            {
                patterns = appSettings["FilesExt"+ i.ToString()].Split(stringSeparators, StringSplitOptions.None);
                HandleFolder(appSettings["RootDir" + i.ToString()], patterns);
            }

            Console.WriteLine(Environment.NewLine + "Finished!!!!! " + ID);
            Console.ReadLine();
        }
       
        private static bool HandleFolder(string path, string[] patterns)
        {
            System.IO.Directory.GetDirectories(path).Select(subDir => HandleFolder(subDir, patterns)).ToArray();
            foreach (string pattern in patterns)
                System.IO.Directory.GetFiles(path, pattern).Select(file => HandleFile(file, path)).AsParallel().ToArray();

            return true;
        }

        private static bool HandleFile(string name, string path)
        {
            List<string> list = new List<string>();

            string code;
            using (System.IO.StreamReader sr = new System.IO.StreamReader(name))
            {
                code = sr.ReadToEnd();
            }

            code = code.Replace('\"', '\'');
            code = code.Replace('\\', ' ');
            code = code.Replace('/', ' ');

            SourceFile sourceFile = new SourceFile
            {
                filename = Path.GetFileName(name),
                directory = path,
                source = code
            };

            Console.WriteLine("Handling file: {0} .", sourceFile.filename);

            string json = SerializeToJson(sourceFile);

            InsertIntoElastic(json, Interlocked.Increment(ref ID));

            return true;
        }

        private static string SerializeToJson(object obj)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(obj.GetType());
            MemoryStream ms = new MemoryStream();
            serializer.WriteObject(ms, obj);
            string retVal = Encoding.UTF8.GetString(ms.ToArray());
            return retVal;
        }

        private static bool InitElastic()
        {
            var appSettings = ConfigurationManager.AppSettings;

            var node = new Uri(
                     "http://" 
                   + appSettings["ServerAddress"]  
                   + ":"
                   + appSettings["ServerPort"]);
            var config = new ConnectionConfiguration(node);
            ElasticClient = new ElasticsearchClient(config);

            // clear database and create a new index
            try
            {
                var result = ElasticClient.IndicesDelete("nim");

                var nimCreateJSon = @"
                            {
                              ""nim"" : {
                                ""mappings"" : {
                                  ""source"" : {
                                    ""properties"" : {
                                      ""directory"" : {
                                        ""type"" : ""string"",
                                        ""index"": ""not_analyzed""
                                      },
                                      ""fileName"" : {
                                        ""type"" : ""string"",
                                        ""index"": ""not_analyzed""
                                      },
                                      ""source"" : {
                                        ""type"" : ""string"",
                                        ""index"": ""not_analyzed""
                                      }
                                    }
                                  }
                                }
                              }
                            }";

                result = ElasticClient.IndicesCreate("nim", nimCreateJSon);
                if (result.HttpStatusCode != 200)
                {
                    return false;
                }

            }
            finally
            {
            }
            return true;
        }

        public static void InsertIntoElastic(object obj, long id)
        {
            try
            {
                ElasticClient.Index("nim", "source", id.ToString(), obj);
            }
            finally
            {
            }
        }
    }
}