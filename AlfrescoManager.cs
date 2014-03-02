using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DotCMIS;
using DotCMIS.Client;
using DotCMIS.Client.Impl;
using DotCMIS.Data.Impl;
using DotCMIS.Enums;
using DotCMIS.Exceptions;

namespace AlfrescoPlayAround
{
    //TODO: cache synch,context and do objects work disconnected!
    public sealed class AlfrescoManager
    {
        private const String BaseUrl = "http://localhost.fiddler:8080/alfresco/service/cmis";
        private readonly Dictionary<String, String> _credentials = new Dictionary<String, String>();
        private readonly SessionFactory _factory = SessionFactory.NewInstance();

        public AlfrescoManager(String userName, String password)
        {
            _credentials.Add(SessionParameter.User, userName);
            _credentials.Add(SessionParameter.Password, password);
            _credentials.Add(SessionParameter.AtomPubUrl, BaseUrl);
            _credentials.Add(SessionParameter.BindingType, BindingType.AtomPub);
        }

        public IList<IRepository> GetRepositories()
        {
            var repos = _factory.GetRepositories(_credentials);
            return repos;
        }

        public Document Get(IRepository repository, String fileName)
        {
            var parameters = GetParameters(repository.Id);

            try
            {
                var session = _factory.CreateSession(parameters);

                var queryString = @"SELECT
                                  cmis:name, cmis:objectId, cmis:baseTypeId, cmis:objectTypeId, cmis:createdBy,
                                  cmis:lastModifiedBy, cmis:lastModificationDate,cmis:contentStreamMimeType,
                                  cmis:contentStreamFileName,cmis:contentStreamId,cmis:contentStreamLength
                                  FROM cmis:document 
                                  Where cmis:name = '" + fileName + "'";

                var result = session.Query(queryString, false).FirstOrDefault();
                var docId = result.GetPropertyValueByQueryName("cmis:objectId").ToString();

                var doc = (Document)session.GetObject(new ObjectId(docId));
                
                session.Clear();

                return doc;

            }
            catch (CmisBaseException baseException)
            {
                var mess = baseException.ErrorContent;

                throw;
            }
        }

        public Document Create(IRepository repository, String folderPath)
        {
            var parameters = GetParameters(repository.Id);

            var session = _factory.CreateSession(parameters);

            IFolder folder = (Folder) session.GetObjectByPath("/" + folderPath);
        
            var properties = new Dictionary<string, object>();
            properties[PropertyIds.Name] = String.Format("hello world {0}", Guid.NewGuid());
            properties[PropertyIds.ObjectTypeId] = "cmis:document";

            var content = Encoding.UTF8.GetBytes(String.Format("Hello World at {0}", DateTime.Now));

            try
            {
                var contentStream = new ContentStream
                                        {
                                            FileName = String.Format("hello-world_{0}.txt", Guid.NewGuid()),
                                            MimeType = "text/plain",
                                            Length = content.Length,
                                            Stream = new MemoryStream(content)
                                        };

                var operationContext = new OperationContext {IncludeAcls = true};

                var doc =
                    (Document)
                    folder.CreateDocument(properties, contentStream, VersioningState.Major, new List<IPolicy>(), null,
                                          null, operationContext);

                session.Clear();

                return doc;
            }
            catch (CmisBaseException baseException)
            {
                var mess = baseException.ErrorContent;

                throw;
            }
        }

        public void Update(IRepository repository, String folderName, Document document)
        {
            var parameters = GetParameters(repository.Id);

            var session = _factory.CreateSession(parameters);

            var properties = new Dictionary<string, object>();
            properties[PropertyIds.Name] = document.ContentStreamFileName;
            properties[PropertyIds.ObjectTypeId] = "cmis:document";

            var content = Encoding.UTF8.GetBytes(String.Format("Hello World at {0}", Guid.NewGuid()));

            var newDoc = (Document) session.GetObject(document.Id);

            if (repository.Capabilities.ContentStreamUpdatesCapability == CapabilityContentStreamUpdates.Anyime)
            {
                //Need to 'check-out' prior to update"
                return;
            }

            var contentStream = new ContentStream
                                    {
                                        FileName = String.Format("Hello World at {0}", Guid.NewGuid()),
                                        MimeType = "text/plain",
                                        Length = content.Length,
                                        Stream = new MemoryStream(content)
                                    };

            newDoc.SetContentStream(contentStream, true);

        }

        public void Delete(IRepository repository, Document document)
        {
            var parameters = GetParameters(repository.Id);

            var session = _factory.CreateSession(parameters);
        
            var newDoc = (Document)session.GetObject(document.Id);

            try
            {

                newDoc.Delete(true);
            }
            catch (CmisBaseException baseException)
            {
                var mess = baseException.ErrorContent;

                throw;
            }
        }

        private Dictionary<String, String> GetParameters(String repositoryId)
        {
            return new Dictionary<String, String>(_credentials)
                       {
                           {
                               SessionParameter.RepositoryId,
                               repositoryId
                           }
                       };
        }
    }
}
