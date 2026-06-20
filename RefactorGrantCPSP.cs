csharp C:\BCCNM CRM Plugin Repo\BCCNM CRM\source\Application\Plugins\BCCNM.Plugins\BCCNM.GrantCPSP\GrantCPSP.cs
using Microsoft.Win32;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.DirectoryServices.ActiveDirectory;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BCCNM_GrantCPSP
{
    public class GrantCPSP : IPlugin
    {
        #region Constants
        /// <summary>
        /// Primary Unit String
        /// </summary>       
        public static readonly int VacuumAssistedEmergencyDelivery = 869750024; 
        public static readonly int EpiduralMaintenance = 869750023;
        public static readonly int InductionandAugmentationofLabour = 869750022;
        public static readonly int SexuallyTransmittedInfectionsManagement = 869750021;
        public static readonly int IntrauterineContraceptionInsertion = 869750020;
        public static readonly int HormonalContraceptiveTherapy = 869750019;
        public static readonly int SurgicalFirstAssistforCesareanSection = 869750018;
        public static readonly int InitialPrescriber = 869750012;
        public static readonly int InitialCPFirstCall = 869750007;
        public static readonly int InitialCPRemoteNurse = 869750006;
        public static readonly int InitialCPOpioidUseDisorderOUD = 869750010;
        public static readonly int InitialCPReproductiveHealthSTI = 869750009;
        public static readonly int InitialCPReproductiveHealthCM = 869750008;

        // Make these dictionaries readonly and static so they are immutable per-instance and safe for reuse by the platform.
        private static readonly Dictionary<string, int> DateTypeCode = new Dictionary<string, int>()
        {
            {"bccnm_isvacuumassistedemergencydelivery", VacuumAssistedEmergencyDelivery},
            {"bccnm_isepiduralmaintenance", EpiduralMaintenance},
            {"bccnm_isinductionandaugmentationoflabour", InductionandAugmentationofLabour},
            {"bccnm_issexuallytransmittedinfectionsmanagement", SexuallyTransmittedInfectionsManagement},
            {"bccnm_isintrauterincontraceptiveinsertion",IntrauterineContraceptionInsertion},
            {"bccnm_ishormonalcontraceptivetherapy",HormonalContraceptiveTherapy },
            {"bccnm_issurgicalfirstassistforcesareansection",SurgicalFirstAssistforCesareanSection },
            {"bccnm_isnonoudprescribingauthority",InitialPrescriber },
            {"bccnm_isrnfirstcall" ,InitialCPFirstCall},
            {"bccnm_isremotenurse",InitialCPRemoteNurse },
            {"bccnm_isopioidusedisorderoud",InitialCPOpioidUseDisorderOUD},
            {"bccnm_isreproductivehealthsti",InitialCPReproductiveHealthSTI},
            {"bccnm_isreproductivehealthcm",InitialCPReproductiveHealthCM }
        };

        private static readonly Dictionary<string,string> DateField = new Dictionary<string, string>(){
            {"bccnm_isvacuumassistedemergencydelivery", "bccnm_practicevacuumassistedemergencydelivedate"},
            {"bccnm_isepiduralmaintenance", "bccnm_practicedateepiduralmaintenancedate"},
            {"bccnm_isinductionandaugmentationoflabour", "bccnm_practiceinductionandaugmentationoflabdate"},
            {"bccnm_issexuallytransmittedinfectionsmanagement","bccnm_practicedatestimanagementdate" },
            {"bccnm_isintrauterincontraceptiveinsertion","bccnm_practiceintrauterincontraceptiveinserdate"},
            {"bccnm_ishormonalcontraceptivetherapy","bccnm_practicehormonalcontraceptivetherapydate" },
            {"bccnm_issurgicalfirstassistforcesareansection","bccnm_practicesurgicalfirstassistforcesareadate" },
            {"bccnm_isnonoudprescribingauthority","bccnm_practicedatenonoudprescribingauthoritydat" },
            {"bccnm_isrnfirstcall" ,"bccnm_practicedaternfirstcalldate"},
            {"bccnm_isremotenurse","bccnm_practicedateremotenursedate" },
            {"bccnm_isopioidusedisorderoud","bccnm_practicedateopioidusedisorderouddate" },
            {"bccnm_isreproductivehealthsti","bccnm_practicedatereproductivehealthstidate" },
            {"bccnm_isreproductivehealthcm","bccnm_practicedatereproductivehealthcmdate" }
        };

        #endregion
        #region Plugin Helper Variables
        // removed instance-local LocalPluginContext field to avoid storing per-request state on the plugin instance.
        #endregion
        #region IPlugin.ExecuteMethod
        /// <summary>
        /// Set schema name of the entity and messages this plugin is expected to registered for.
        /// </summary>
        /// <param name="serviceProvider"></param>
        public void Execute(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException("serviceProvider");
            }

            var localContext = new LocalPluginContext(serviceProvider);

            localContext.Trace(string.Format(CultureInfo.InvariantCulture, "Entered {0}.Execute()", this.ChildClassName));

            try
            {
                // Iterate over all of the expected registered events to ensure that the plugin
                // has been invoked by an expected event
                // For any given plug-in event at an instance in time, we would expect at most 1 result to match.
                Action<LocalPluginContext> entityAction =
                    (from result in this.RegisteredEvents
                     where (
                            result.Item1 == localContext.PluginExecutionContext.Stage &&
                            result.Item2 == localContext.PluginExecutionContext.MessageName &&
                            (string.IsNullOrWhiteSpace(result.Item3) ? true : result.Item3 == localContext.PluginExecutionContext.PrimaryEntityName)
                    )
                     select result.Item4).FirstOrDefault();

                if (entityAction != null)
                {
                    localContext.Trace(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} is firing for Entity: {1}, Message: {2}",
                        this.ChildClassName,
                        localContext.PluginExecutionContext.PrimaryEntityName,
                        localContext.PluginExecutionContext.MessageName));
                    entityAction.Invoke(localContext);
                    // now exit - if the derived plug-in has incorrectly registered overlapping event registrations,
                    // guard against multiple executions.
                    return;
                }

                //Execute the plugin logic
                RunPlugin(localContext);
            }
            catch (Exception e)
            {
                localContext.Trace(string.Format(CultureInfo.InvariantCulture, "Exception: {0}", e.ToString()));
                throw new InvalidPluginExecutionException(e.Message);
            }
            finally
            {
                localContext.Trace(string.Format(CultureInfo.InvariantCulture, "Exiting {0}.Execute()", this.ChildClassName));
            }
        }

        #endregion
        #region RunPlugin
        /// <summary>
        /// Execute logic on create and update of Customer Address
        /// </summary>
        /// <param name="localContext">Local Context</param>
        private void RunPlugin(LocalPluginContext localContext)
        {
            #region TRY
            try
            {
                #region PluginCode
                //////////////////////////////////////////////////////////////////////////////////////////  

                #region GetParameters                                            
                EntityReference certifiedSpecializedRecordReference = (EntityReference)localContext.PluginExecutionContext.InputParameters["cpsp"];
                localContext.Trace("certified Specialized Record Reference - " + certifiedSpecializedRecordReference.Id);
                EntityReference existingLicenceStateReference = (EntityReference)localContext.PluginExecutionContext.InputParameters["existingLicenceState"];
                localContext.Trace("Existing licence status Record Reference - " + existingLicenceStateReference.Id);
                string fieldName = localContext.PluginExecutionContext.InputParameters["fieldNameOption"].ToString();
                localContext.Trace("Field Name - " + fieldName);
                localContext.Trace("Input Parameters retrieved.");

                Entity existingRegistrationStatusRecord = localContext.AdminOrganizationService.Retrieve("dllt_registrationcertificate", existingLicenceStateReference.Id, new ColumnSet(true));
                Entity certifiedSpecializedRecord = localContext.AdminOrganizationService.Retrieve("bccnm_certifiedspecializedpractice", certifiedSpecializedRecordReference.Id, new ColumnSet("bccnm_relatedapplicationid"));
                EntityReference memberReference = (EntityReference)existingRegistrationStatusRecord["dllt_member"];
                EntityReference applicationReference = (EntityReference)certifiedSpecializedRecord["bccnm_relatedapplicationid"];

                #region Create Registration Status
                var newGuid = CreateRegistrationStatusRecord(existingRegistrationStatusRecord, fieldName, certifiedSpecializedRecord, localContext);
                #endregion

                #region Update Existing Registration Status
                UpdateRegistrationStatusRecord(existingLicenceStateReference.Id, newGuid, localContext);
                #endregion

                #region Update CPSP Status to Granted
                UpdateCPSP(certifiedSpecializedRecordReference.Id, localContext);
                #endregion

                #region Create Registration Date
                CreateRegistrationDateRecord(existingLicenceStateReference.Id, existingRegistrationStatusRecord, fieldName, localContext);
                #endregion

                #region Update Application to Granted
                Guid AppId = ((EntityReference)certifiedSpecializedRecord["bccnm_relatedapplicationid"]).Id;
                UpdateApplicationStatus(AppId, localContext);
                #endregion

                #region Create Portal Msg
                #region Get Subject area and Topic
                localContext.Trace("Entered creation of Portal msg");
                Entity msgSubjectArea = GetMsgSubj(localContext.AdminOrganizationService, "00007");
                Entity msgTopic = GetMsgSubj(localContext.AdminOrganizationService, "00015");
                string portalUrl = GetEnvironmaneVar(localContext.AdminOrganizationService, "Portal URL") + "start-new-submission/";
                string hyperlink = "<a href='" + portalUrl + "' target=_parent>apply for prescribing authority</a>";
                localContext.Trace("Retrieved subject, topis and portal url");
                var CPorSPpracticeArea = "";
                #endregion
                if (fieldName == "bccnm_isopioidusedisorderoud") //OUD
                {
                    createPortalMessage(localContext, memberReference, applicationReference, memberReference.Id.ToString(), 
                        "BCCNM: Grant CP/SP - OUD", CPorSPpracticeArea, hyperlink);
                    localContext.Trace("msg for OUD completed");
                }
                if (fieldName == "bccnm_isreproductivehealthcm" || fieldName == "bccnm_isreproductivehealthsti" || fieldName == "bccnm_isremotenurse" || fieldName == "bccnm_isrnfirstcall")
                { 
                    //cp other
                    CPorSPpracticeArea = "";
                    createPortalMessage(localContext, memberReference, applicationReference, memberReference.Id.ToString(),
                        "BCCNM: Grant CP/SP - Other than OUD", CPorSPpracticeArea, hyperlink);
                    localContext.Trace("msg for other than OUD completed");
                }
                if (fieldName == "bccnm_issurgicalfirstassistforcesareansection" || fieldName == "bccnm_isintrauterincontraceptiveinsertion" || fieldName == "")
                {
                    //SP
                    switch (fieldName)
                    {
                        case "bccnm_issurgicalfirstassistforcesareansection":
                            CPorSPpracticeArea = "Surgical Assist";
                            break;
                        case "bccnm_isintrauterincontraceptiveinsertion":
                            CPorSPpracticeArea = "Intrauterine Contraception Insertion";
                            break;
                    }
                    createPortalMessage(localContext, memberReference, applicationReference, memberReference.Id.ToString(),
                        "BCCNM: Grant CP/SP - Specialized for Acu, SA, ICI", CPorSPpracticeArea, hyperlink);
                    localContext.Trace("msg for SP 1 completed");
                }
                if (fieldName == "bccnm_ishormonalcontraceptivetherapy" || fieldName == "bccnm_issexuallytransmittedinfectionsmanagement" || fieldName == "bccnm_isepiduralmaintenance")
                {
                    //SP
                    switch (fieldName)
                    {
                        case "bccnm_ishormonalcontraceptivetherapy":
                            CPorSPpracticeArea = "hormonal contraceptive therapy";
                            break;
                        case "bccnm_issexuallytransmittedinfectionsmanagement":
                            CPorSPpracticeArea = "sexually transmitted infections management";
                            break;
                        case "bccnm_isepiduralmaintenance":
                            CPorSPpracticeArea = "epidural maintenance";
                            break;
                    }
                    createPortalMessage(localContext, memberReference, applicationReference, memberReference.Id.ToString(),
                        "BCCNM: Grant CP/SP - Specialized for HCM, STI, EM", CPorSPpracticeArea, hyperlink);
                    localContext.Trace("msg for SP 2 completed");
                }
                if(fieldName == "bccnm_isnonoudprescribingauthority")
                {
                    //Non-OUD
                    createPortalMessage(localContext, memberReference, applicationReference, memberReference.Id.ToString(),
                        "BCCNM: Grant CP/SP - Certified Prescribing authority", CPorSPpracticeArea, hyperlink);
                    localContext.Trace("msg for Non-OUD completed");
                }
                #endregion

                #endregion
                #region Declare and Instantiate Objects
                #endregion

                #endregion
            }
            #endregion
            #region CATCH
            catch (Exception exception) //Catch and throw all exception as InvalidPluginExecutionException
            {
                throw new InvalidPluginExecutionException(exception.Message, exception);
            }
            #endregion

        }
        #endregion
        #region Create Registration Status Record
        /// <summary>
        /// Create Registration Status
        /// </summary>
        /// <param name="RegistrationStatus"></param>
        /// <param name="fieldName"></param>
        /// <param name="certifiedSpecializedRecord"></param>
        public Guid CreateRegistrationStatusRecord(Entity existingRegistrationStatusRecord,string fieldName, Entity certifiedSpecializedRecord, LocalPluginContext ctx)
        {
            ctx.Trace($"Executing [{MethodBase.GetCurrentMethod().Name}]...");
            ctx.Trace("Attribute count - " +existingRegistrationStatusRecord.Attributes.Count.ToString());
            Entity registrationCertificate = new Entity("dllt_registrationcertificate");
            registrationCertificate["dllt_member"] = existingRegistrationStatusRecord["dllt_member"];//
            registrationCertificate["statuscode"] = existingRegistrationStatusRecord["statuscode"];//
            registrationCertificate["bccnm_isauthorizedtopractice"] = existingRegistrationStatusRecord["bccnm_isauthorizedtopractice"];//
            registrationCertificate["dllt_registrationclasseffectivefromdate"] = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time"));//
            registrationCertificate["dllt_registrationstatuseffectivedate"] = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time"));
            registrationCertificate["bccnm_relatedapplicationid"] = certifiedSpecializedRecord["bccnm_relatedapplicationid"];
            registrationCertificate["dllt_relatedregistrationstatusid"] = existingRegistrationStatusRecord["dllt_relatedregistrationstatusid"];//
            registrationCertificate["dllt_relatedregistrationclassid"] = existingRegistrationStatusRecord["dllt_relatedregistrationclassid"];//
            registrationCertificate["dllt_relatedregistrationsubclassid"] = existingRegistrationStatusRecord["dllt_relatedregistrationsubclassid"];//
            registrationCertificate["dllt_relatedapprovaltypeid"] = existingRegistrationStatusRecord["dllt_relatedapprovaltypeid"];//
            registrationCertificate["ownerid"] = existingRegistrationStatusRecord["ownerid"];//
            registrationCertificate[fieldName] = true;
            registrationCertificate[DateField[fieldName]] = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time"));//
            if (fieldName == "bccnm_isopioidusedisorderoud" || fieldName == "bccnm_isnonoudprescribingauthority")
            {
                registrationCertificate["bccnm_prescribingauthoritycode"] = new OptionSetValue(1);
            }
            registrationCertificate["bccnm_relatedregistrationcertificateid"] = new EntityReference("dllt_registrationcertificate", existingRegistrationStatusRecord.Id);
            registrationCertificate["bccnm_iscertifiedpracticeachieved"] = existingRegistrationStatusRecord.Contains("bccnm_iscertifiedpracticeachieved") ? existingRegistrationStatusRecord["bccnm_iscertifiedpracticeachieved"] : null;
            registrationCertificate["bccnm_havepharmanetaccesscode"] = existingRegistrationStatusRecord.Contains("bccnm_havepharmanetaccesscode") ? existingRegistrationStatusRecord["bccnm_havepharmanetaccesscode"] : null;
            registrationCertificate["bccnm_mspeligiblecode"] = existingRegistrationStatusRecord.Contains("bccnm_mspeligiblecode") ? existingRegistrationStatusRecord["bccnm_mspeligiblecode"] : null;
            registrationCertificate["bccnm_allowprescriptionpadorderingcode"] = existingRegistrationStatusRecord.Contains("bccnm_allowprescriptionpadorderingcode") ? existingRegistrationStatusRecord["bccnm_allowprescriptionpadorderingcode"] : null;
            registrationCertificate["bccnm_pharamnetid"] = existingRegistrationStatusRecord.Contains("bccnm_pharamnetid") ? existingRegistrationStatusRecord["bccnm_pharamnetid"] : null;
            
            Guid newReg = CreateRecord(registrationCertificate, ctx);
            return newReg;
        }
        #endregion
        #region Update Existing Registration Status Record
        /// <summary>
        /// Update Registration Status
        /// </summary>
        /// <param name="RegistrationStatus"></param>
        /// <param name="newGuid"></param>
        public void UpdateRegistrationStatusRecord(Guid RegistrationStatus,Guid newGuid, LocalPluginContext ctx)
        {
            ctx.Trace($"Executing [{MethodBase.GetCurrentMethod().Name}]...");
            Entity ExregistrationCertificate = new Entity("dllt_registrationcertificate");
            ExregistrationCertificate.Id = RegistrationStatus;
            ExregistrationCertificate["bccnm_relatedregistrationcertificatid"] = new EntityReference("dllt_registrationcertificate", newGuid);
            ExregistrationCertificate["dllt_registrationclasseffectivetodate"] = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time"));
            UpdateRecord(ExregistrationCertificate, ctx);

        }
        #endregion
        #region Update CPSP Status to Granted
        /// <summary>
        /// Update CPSP
        /// </summary>
        /// <param name="CPSPrecord"></param>
        public void UpdateCPSP(Guid CPSPrecord, LocalPluginContext ctx)
        {
            ctx.Trace($"Executing [{MethodBase.GetCurrentMethod().Name}]...");
            Entity cpsp = new Entity("bccnm_certifiedspecializedpractice");
            cpsp.Id = CPSPrecord;
            cpsp["statuscode"] = new OptionSetValue(2);
            cpsp["statecode"] = new OptionSetValue(1);
            UpdateRecord(cpsp, ctx);
        }
        #endregion
        #region Create Registration Date
        /// <summary>
        /// Create Registration Date
        /// </summary>
        /// <param name="RegistrationStatus"></param>
        /// <param name="existingRegistrationStatusRecord"></param>
        /// <param name="fieldName"></param>
        public Guid CreateRegistrationDateRecord(Guid RegistrationStatus, Entity existingRegistrationStatusRecord,string fieldName, LocalPluginContext ctx)
        {
            ctx.Trace($"Executing [{MethodBase.GetCurrentMethod().Name}]....");
            Entity registrationDate = new Entity("bccnm_registrationdate");
            registrationDate["bccnm_datetypecode"] = new OptionSetValue(DateTypeCode[fieldName]);
            registrationDate["bccnm_relatedregistrationgroupid"] = existingRegistrationStatusRecord["dllt_relatedregistrationclassid"];
            registrationDate["bccnm_date"] = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time"));////existingRegistrationStatusRecord["dllt_registrationclasseffectivefromdate"];
            registrationDate["bccnm_relatedcontactid"] = existingRegistrationStatusRecord["dllt_member"];

            Guid newReg = CreateRecord(registrationDate, ctx);
            return newReg;
        }
        #endregion
        #region Update Application Status to Granted
        /// <summary>
        /// Update Application
        /// </summary>
        /// <param name="CPSPrecord"></param>
        public void UpdateApplicationStatus(Guid Apprecord, LocalPluginContext ctx)
        {
            ctx.Trace($"Executing [{MethodBase.GetCurrentMethod().Name}]...");
            ctx.Trace($"AppRecord" + Apprecord);

            var fetchXml = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
  <entity name='bccnm_certifiedspecializedpractice'>
    <attribute name='bccnm_certifiedspecializedpracticeid' />
    <attribute name='bccnm_name' />
    <attribute name='createdon' />
    <order attribute='bccnm_name' descending='false' />
    <filter type='and'>
      <condition attribute='bccnm_relatedapplicationid' operator='eq' value='" + Apprecord + @"' />
      <condition attribute='statuscode' operator='ne' value='2' />
    </filter>
  </entity>
</fetch>";
            var results = ctx.AdminOrganizationService.RetrieveMultiple(new FetchExpression(fetchXml));
            ctx.Trace($"- Retrieved [{results.Entities.Count}] of [{results.EntityName}] records.");
            int regClassCount = results.Entities.Count;
            if (regClassCount == 0)
            {
                Entity App = new Entity("dllt_application");
                App.Id = Apprecord;
                App["statuscode"] = new OptionSetValue(904710016);
                App["statecode"] = new OptionSetValue(1);
                UpdateRecord(App, ctx);
            }
        }
        #endregion
        #region Get Environment Variable
        public string GetEnvironmaneVar(IOrganizationService service, string FromEmailIDForNotification)
        {
            string envVarEntity = null;
            string envVarFetch = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
								<entity name='environmentvariabledefinition'>
								<attribute name='environmentvariabledefinitionid' />
								<attribute name='defaultvalue' />
								<order attribute='schemaname' descending='false' />
								<filter type='and'>
								<condition attribute='displayname' operator='eq' value='" + FromEmailIDForNotification + @"' />
								</filter>
								</entity>
								</fetch>";
            EntityCollection userColl = service.RetrieveMultiple(new FetchExpression(envVarFetch));
            if (userColl.Entities.Count > 0)
            {
                envVarEntity = userColl.Entities.FirstOrDefault().GetAttributeValue<string>("defaultvalue");
            }
            return envVarEntity;
        }
        #endregion
        #region Create portal message
        /// <summary>
        /// Create Portal message and publish
        /// </summary>
        /// <param name="memberReference"></param>
        /// <param name="applicationReference"></param>
        /// <returns></returns>
        public void createPortalMessage(LocalPluginContext ctx, EntityReference memberReference, EntityReference applicationReference, string userId,
           string emailTempName, string CPorSPpracticeArea, string hyperlink)
        {
            ctx.Trace("Started portal msg creation function");
            string nursingRegisterLink = "";
            string midwiferyRegisterLink = "";
            Entity contactEntity = ctx.AdminOrganizationService.Retrieve("contact", memberReference.Id, new Microsoft.Xrm.Sdk.Query.ColumnSet("bccnm_communicationname","firstname"));
            string communicationName = contactEntity.Attributes.Contains("bccnm_communicationname") ? contactEntity["bccnm_communicationname"].ToString() : contactEntity["firstname"].ToString();
            Entity emailTemplateRecord = GetEmailTemplate(ctx.AdminOrganizationService, emailTempName);
            string emailSub = "";
            string emailBody = "";

            if (emailTemplateRecord != null && emailTemplateRecord.Contains("safehtml") && emailTemplateRecord.Contains("subjectsafehtml"))
            {
                emailBody = emailTemplateRecord.GetAttributeValue<string>("safehtml").Replace("[communication name]", communicationName).Replace("[nursing register]", nursingRegisterLink).
                    Replace("[midwifery register]", midwiferyRegisterLink).Replace("[SP practice area]", CPorSPpracticeArea).Replace("[CP practice area]", CPorSPpracticeArea).
                    Replace("[apply for prescribing authority]", hyperlink);
                emailSub = emailTemplateRecord.GetAttributeValue<string>("subjectsafehtml");
            }
            Entity fromActivityParty = new Entity("activityparty");
            Entity userNoReply = GetNOReplyUserConnects(ctx.AdminOrganizationService);
            if (userNoReply != null)
            {
                fromActivityParty["partyid"] = userNoReply.ToEntityReference();
            }
            else
            {
                fromActivityParty["partyid"] = new EntityReference("systemuser", new Guid(userId));
            }

            Entity toActivityParty = new Entity("activityparty");
            toActivityParty["partyid"] = new EntityReference("contact", memberReference.Id);

            ctx.Trace("All variables set on portal msg creation");

            Entity CreatePortalMsg = new Entity("dllt_portalmessage");
            CreatePortalMsg["from"] = new Entity[] { fromActivityParty };
            CreatePortalMsg["to"] = new Entity[] { toActivityParty };
            // Use service-fetched subject area/topic
            Entity msgSubjectArea = GetMsgSubj(ctx.AdminOrganizationService, "00007");
            Entity msgTopic = GetMsgSubj(ctx.AdminOrganizationService, "00015");
            if (msgSubjectArea != null) CreatePortalMsg["bccnm_relatedsubjectareaid"] = new EntityReference("bccnm_messagesubjectarea", msgSubjectArea.Id);
            if (msgTopic != null) CreatePortalMsg["bccnm_relatedtopicid"] = new EntityReference("bccnm_messagesubjectarea", msgTopic.Id);
            CreatePortalMsg["subject"] = emailSub;
            CreatePortalMsg["dllt_messagemulti"] = emailBody;
            CreatePortalMsg["regardingobjectid"] = new EntityReference("dllt_application", applicationReference.Id);
            CreatePortalMsg["dllt_message_receiver"] = new EntityReference("contact", memberReference.Id);
            CreatePortalMsg["statecode"] = new OptionSetValue(1);
            CreatePortalMsg["dllt_relatedcustomerid"] = new EntityReference("contact", memberReference.Id);
            var portalmsgID = CreateRecord(CreatePortalMsg, ctx);

            ctx.Trace("Creation of portal msg completed");

            Entity UpdateePortalMsg = new Entity("dllt_portalmessage");
            UpdateePortalMsg.Id = portalmsgID;
            UpdateePortalMsg["statecode"] = new OptionSetValue(0);
            UpdateePortalMsg["statuscode"] = new OptionSetValue(904710000);
            UpdateePortalMsg["dllt_sentdate"] = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time"));
            UpdateRecord(UpdateePortalMsg, ctx);
        }
        #endregion
        #region Get NoReply user
        public Entity GetNOReplyUserConnects(IOrganizationService service)
        {
            string emailAddress = GetEnvironmaneVar(service, "FromEmailIDForNotification");
            Entity userEntity = null;
            string userFetch = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                  <entity name='systemuser'>
                                    <attribute name='fullname' />
                                    <attribute name='businessunitid' />
                                    <attribute name='title' />
                                    <attribute name='address1_telephone1' />
                                    <attribute name='positionid' />
                                    <attribute name='systemuserid' />
                                    <order attribute='fullname' descending='false' />
                                    <filter type='and'>
                                      <condition attribute='internalemailaddress' operator='eq' value='" + emailAddress + @"' />
                                    </filter>
                                  </entity>
                                </fetch>";
            EntityCollection userColl = service.RetrieveMultiple(new FetchExpression(userFetch));
            if (userColl.Entities.Count > 0)
            {
                userEntity = userColl.Entities.FirstOrDefault();
            }
            return userEntity;
        }
        #endregion
        #region Get Email Template
        public Entity GetEmailTemplate(IOrganizationService service,string emailName)
        {
            Entity emailTempEntity = null;
            var emailTemplateFetch = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                  <entity name='template'>
                                                    <attribute name='title' />
                                                    <attribute name='templateid' />
                                                    <attribute name='subjectsafehtml' />
                                                    <attribute name='body' />
                                                    <attribute name='safehtml' />
                                                    <order attribute='title' descending='false' />
                                                    <filter type='and'>
                                                      <condition attribute='title' operator='eq' value='"+ emailName+@"' />
                                                    </filter>
                                                  </entity>
                                                </fetch>";
            EntityCollection emailTemplateColl = service.RetrieveMultiple(new FetchExpression(emailTemplateFetch));
            if (emailTemplateColl.Entities.Count > 0)
            {
                emailTempEntity = emailTemplateColl.Entities.FirstOrDefault();
            }
            return emailTempEntity;
        }
        #endregion
        #region Get Message subject area
        public Entity GetMsgSubj(IOrganizationService service, string templateName)
        {
            Entity msgSubEntity = null;
            var msgSubFetch = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
  <entity name='bccnm_messagesubjectarea'>
    <attribute name='bccnm_messagesubjectareaid' />
    <attribute name='bccnm_name' />
    <attribute name='createdon' />
    <order attribute='bccnm_name' descending='false' />
    <filter type='and'>
      <condition attribute='bccnm_id' operator='eq' value='" + templateName + @"' />
    </filter>
  </entity>
</fetch>";
            EntityCollection msgSubColl = service.RetrieveMultiple(new FetchExpression(msgSubFetch));
            if (msgSubColl.Entities.Count > 0)
            {
                msgSubEntity = msgSubColl.Entities.FirstOrDefault();
            }
            return msgSubEntity;
        }
        #endregion
        #region Create Record
        /// <summary>
        /// Create Record and return GUID
        /// </summary>
        /// <param name="entityRecord"></param>
        /// <returns></returns>
        public Guid CreateRecord(Entity entityRecord, LocalPluginContext ctx)
        {
            ctx.Trace($"Executing [{MethodBase.GetCurrentMethod().Name}]...");
            Guid ID = ctx.AdminOrganizationService.Create(entityRecord);
            return ID;
        }
        #endregion
        #region Update Record
        /// <summary>
        /// Update Record
        /// </summary>
        /// <param name="entityRecord"></param>
        public void UpdateRecord(Entity entityRecord, LocalPluginContext ctx)
        {
            ctx.Trace($"Executing [{MethodBase.GetCurrentMethod().Name}]...");
            ctx.AdminOrganizationService.Update(entityRecord);
        }
        #endregion
        #region Constructors 
        protected string ChildClassName
        {
            get;
        }
        #endregion
        #region ContextAndSettingsClasss
        protected class LocalPluginContext
        {
            internal IOrganizationService OrganizationService
            {
                get;

                private set;
            }

            internal IOrganizationService AdminOrganizationService
            {
                get;

                private set;
            }

            internal IPluginExecutionContext PluginExecutionContext
            {
                get;

                private set;
            }

            internal ITracingService TracingService
            {
                get;

                private set;
            }

            private LocalPluginContext()
            {
            }

            internal LocalPluginContext(IServiceProvider serviceProvider)
            {
                if (serviceProvider == null)
                {
                    throw new ArgumentNullException("serviceProvider");
                }

                // Obtain the execution context service from the service provider.
                this.PluginExecutionContext = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

                // Obtain the tracing service from the service provider.
                this.TracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

                // Obtain the Organization Service factory service from the service provider
                IOrganizationServiceFactory factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));

                // Use the factory to generate the Organization Service.
                this.OrganizationService = factory.CreateOrganizationService(this.PluginExecutionContext.UserId);

                // Use the factory to generate the Admin Organization Service.
                this.AdminOrganizationService = factory.CreateOrganizationService(null);
            }

            internal void Trace(string message)
            {
                if (string.IsNullOrWhiteSpace(message) || this.TracingService == null)
                {
                    return;
                }

                if (this.PluginExecutionContext == null)
                {
                    this.TracingService.Trace(message);
                }
                else
                {
                    this.TracingService.Trace(
                        "{0}, Correlation Id: {1}, Initiating User: {2}",
                        message,
                        this.PluginExecutionContext.CorrelationId,
                        this.PluginExecutionContext.InitiatingUserId);
                }
            }
        }

        private Collection<Tuple<int, string, string, Action<LocalPluginContext>>> registeredEvents;

        protected Collection<Tuple<int, string, string, Action<LocalPluginContext>>> RegisteredEvents
        {
            get
            {
                if (this.registeredEvents == null)
                {
                    this.registeredEvents = new Collection<Tuple<int, string, string, Action<LocalPluginContext>>>();
                }

                return this.registeredEvents;
            }
        }

        #endregion
    }
}