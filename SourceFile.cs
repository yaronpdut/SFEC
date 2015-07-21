using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SourceFilesElasticCrawler
{
    public struct SourceFile
    {
        public string filename { get; set; }
        public string directory { get; set; }
        public string source { get; set; }
    }
}