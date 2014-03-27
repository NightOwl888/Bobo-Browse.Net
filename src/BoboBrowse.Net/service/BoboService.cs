using System.IO;
using BoboBrowse.Api;
using log4net;
using Lucene.Net.Index;
using Lucene.Net.Store;

namespace BoboBrowse.service
{
    public class BoboService
    {
        private static log4net.ILog logger = LogManager.GetLogger(typeof(BoboService));

        private readonly FileInfo idxDir;
        private BoboIndexReader boboReader;

        public BoboService(string path)
            : this(new FileInfo(path))
        {
        }

        public BoboService(FileInfo idxDir)
        {
            this.idxDir = idxDir;
            boboReader = null;
        }

        public virtual BrowseResult Browse(BrowseRequest req)
        {
            BoboBrowser browser = null;
            try
            {
                browser = new BoboBrowser(boboReader);
                return browser.Browse(req);
            }
            catch (BrowseException be)
            {
                logger.Error(be.Message, be);
                return new BrowseResult();
            }
            finally
            {
                if (browser != null)
                {
                    try
                    {
                        browser.Close();
                    }
                    catch (IOException e)
                    {
                        logger.Error(e.Message);
                    }
                }
            }
        }

        public virtual void Start()
        {
            IndexReader reader = IndexReader.Open(FSDirectory.GetDirectory(idxDir), true);
            try
            {
                boboReader = BoboIndexReader.GetInstance(reader);
            }
            catch (IOException ioe)
            {
                if (reader != null)
                {
                    reader.Close();
                }
            }
        }

        public virtual void Shutdown()
        {
            if (boboReader != null)
            {
                try
                {
                    boboReader.Close();
                }
                catch (IOException e)
                {
                    logger.Error(e.Message);
                }
            }
        }
    }
}