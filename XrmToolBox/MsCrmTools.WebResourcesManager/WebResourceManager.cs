﻿// PROJECT : MsCrmTools.WebResourcesManager
// This project was developed by Tanguy Touzard
// CODEPLEX: http://xrmtoolbox.codeplex.com
// BLOG: http://mscrmtools.blogspot.com

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using MsCrmTools.WebResourcesManager.AppCode;

namespace MsCrmTools.WebResourcesManager
{
    /// <summary>
    /// Class that manages action on web resources
    /// in Microsoft Dynamics CRM 2011
    /// </summary>
    internal class WebResourceManager
    {
        #region Variables
        
        /// <summary>
        /// Xrm Organization service
        /// </summary>
        readonly IOrganizationService innerService;

        #endregion Variables

        #region Constructor

        /// <summary>
        /// Initializes a new instance of class WebResourceManager
        /// </summary>
        /// <param name="service">Xrm Organization service</param>
        public WebResourceManager(IOrganizationService service)
        {
            innerService = service;
        }

        #endregion Constructor

        #region Methods

        /// <summary>
        /// Retrieves all web resources that are customizable
        /// </summary>
        /// <returns>List of web resources</returns>
        internal EntityCollection RetrieveWebResources(Guid solutionId)
        {
            try
            {
                if (solutionId == Guid.Empty)
                {
                    var qba = new QueryByAttribute("webresource") {ColumnSet = new ColumnSet(true)};
                    qba.Attributes.AddRange(new[] {"ishidden", "iscustomizable"});
                    qba.Values.AddRange(new object[] {false, true});
                    qba.Orders.Add(new OrderExpression("name", OrderType.Ascending));

                    return innerService.RetrieveMultiple(qba);
                }
                else
                {
                    var qba = new QueryByAttribute("solutioncomponent") {ColumnSet = new ColumnSet(true)};
                    qba.Attributes.AddRange(new[] { "solutionid", "componenttype" });
                    qba.Values.AddRange(new object[] { solutionId, 61 });

                    var components = innerService.RetrieveMultiple(qba).Entities;

                    var list = components.Select(component => component.GetAttributeValue<Guid>("objectid").ToString("B")).ToList();

                    var qe = new QueryExpression("webresource") {ColumnSet = new ColumnSet(true)};
                    qe.Criteria.AddCondition("webresourceid", ConditionOperator.In, list.ToArray());
                    return innerService.RetrieveMultiple(qe);
                }
            }
            catch (Exception error)
            {
                throw new Exception("Error while retrieving web resources: " + error.Message);
            }
        }

        /// <summary>
        /// Retrieves a specific web resource from its unique identifier
        /// </summary>
        /// <param name="webresourceId">Web resource unique identifier</param>
        /// <returns>Web resource</returns>
        internal Entity RetrieveWebResource(Guid webresourceId)
        {
            try
            {
                return innerService.Retrieve("webresource", webresourceId, new ColumnSet(true));
            }
            catch (Exception error)
            {
                throw new Exception("Error while retrieving web resource: " + error.Message);
            }
        }

        /// <summary>
        /// Retrieves a specific web resource from its unique name
        /// </summary>
        /// <param name="name">Web resource unique name</param>
        /// <returns>Web resource</returns>
        internal Entity RetrieveWebResource(string name)
        {
            try
            {
                var qba = new QueryByAttribute("webresource");
                qba.Attributes.Add("name");
                qba.Values.Add(name);
                qba.ColumnSet = new ColumnSet(true);

                EntityCollection collection = innerService.RetrieveMultiple(qba);

                if (collection.Entities.Count == 0)
                {
                    return null;
                }

                if (collection.Entities.Count > 1)
                {
                    throw new Exception(string.Format("there are more than one web resource with name '{0}'", name));
                }

                return collection[0];
            }
            catch (Exception error)
            {
                throw new Exception("Error while retrieving web resource: " + error.Message);
            }
        }

        /// <summary>
        /// Updates the provided web resource
        /// </summary>
        /// <param name="script">Web resource to update</param>
        internal Guid UpdateWebResource(Entity script)
        {
            try
            {
                if (!script.Contains("webresourceid"))
                {
                    Entity existingEntity = RetrieveWebResource(script["name"].ToString());

                    if (existingEntity == null)
                        return CreateWebResource(script);

                    script.Id = existingEntity.Id;

                    if (!script.Contains("displayname") && existingEntity.Contains("displayname"))
                        script["displayname"] = existingEntity["displayname"];

                    if (!script.Contains("description") && existingEntity.Contains("description"))
                        script["description"] = existingEntity["description"];

                    innerService.Update(script);
                    return script.Id;
                }

                innerService.Update(script);
                return script.Id;
            }
            catch (Exception error)
            {
                throw new Exception("Error while updating web resource: " + error.Message);
            }
        }

        /// <summary>
        /// Deletes the provided web resource
        /// </summary>
        /// <param name="webResource">Web resource to delete</param>
        internal void DeleteWebResource(Entity webResource)
        {
            try
            {
                innerService.Delete(webResource.LogicalName, webResource.Id);
            }
            catch (Exception error)
            {
                throw new Exception("Error while deleting web resource: " + error.Message);
            }
        }

        /// <summary>
        /// Creates the provided web resource
        /// </summary>
        /// <param name="webResource">Web resource to create</param>
        internal Guid CreateWebResource(Entity webResource)
        {
            try
            {
                return innerService.Create(webResource);
            }
            catch (Exception error)
            {
                throw new Exception("Error while creating web resource: " + error.Message);
            }
        }

        internal void PublishWebResources(List<Guid> ids)
        {
            try
            {
                string idsXml = string.Empty;

                foreach (Guid id in ids)
                {
                    idsXml += string.Format("<webresource>{0}</webresource>", id.ToString("B"));
                }

                var pxReq1 = new PublishXmlRequest
                {
                    ParameterXml = String.Format("<importexportxml><webresources>{0}</webresources></importexportxml>", idsXml)
                };

                innerService.Execute(pxReq1);
            }
            catch (Exception error)
            {
                throw new Exception("Error while publishing web resources: " + error.Message);
            }
        }

        internal void AddToSolution(List<Guid> idsToPublish, string solutionUniqueName)
        {
            foreach (var id in idsToPublish)
            {
                var request = new AddSolutionComponentRequest
                                  {
                                      AddRequiredComponents = false,
                                      ComponentId = id,
                                      ComponentType = SolutionComponentType.WebResource,
                                      SolutionUniqueName = solutionUniqueName
                                  };

                innerService.Execute(request);
            }
        }

        internal static string GetContentFromBase64String(string base64)
        {
            byte[] b = Convert.FromBase64String(base64);
            return System.Text.Encoding.UTF8.GetString(b);
        }

        internal static string GetBase64StringFromString(string content)
        {
            byte[] byt = System.Text.Encoding.UTF8.GetBytes(content);
            return Convert.ToBase64String(byt);
        }

        internal bool HasDependencies(Guid webresourceId)
        {
            var request = new RetrieveDependenciesForDeleteRequest
                              {
                                  ComponentType = SolutionComponentType.WebResource,
                                  ObjectId = webresourceId
                              };

            var response = (RetrieveDependenciesForDeleteResponse)innerService.Execute(request);
            return response.EntityCollection.Entities.Count != 0;
        }

        #endregion
    }
}
