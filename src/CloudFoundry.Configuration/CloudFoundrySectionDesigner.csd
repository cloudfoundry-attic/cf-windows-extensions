<?xml version="1.0" encoding="utf-8"?>
<configurationSectionModel xmlns:dm0="http://schemas.microsoft.com/VisualStudio/2008/DslTools/Core" dslVersion="1.0.0.0" Id="fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d" namespace="CloudFoundry.Configuration" xmlSchemaNamespace="urn:CloudFoundry.Configuration" xmlns="http://schemas.microsoft.com/dsltools/ConfigurationSectionDesigner">
  <typeDefinitions>
    <externalType name="String" namespace="System" />
    <externalType name="Boolean" namespace="System" />
    <externalType name="Int32" namespace="System" />
    <externalType name="Int64" namespace="System" />
    <externalType name="Single" namespace="System" />
    <externalType name="Double" namespace="System" />
    <externalType name="DateTime" namespace="System" />
    <externalType name="TimeSpan" namespace="System" />
  </typeDefinitions>
  <configurationElements>
    <configurationSection name="CloudFoundrySection" namespace="CloudFoundry.Configuration" codeGenOptions="Singleton, XmlnsProperty" xmlSectionName="cloudfoundry">
      <elementProperties>
        <elementProperty name="DEA" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="dea" isReadOnly="false">
          <type>
            <configurationElementMoniker name="/fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d/DEAElement" />
          </type>
        </elementProperty>
      </elementProperties>
    </configurationSection>
    <configurationElement name="DEAElement">
      <attributeProperties>
        <attributeProperty name="BaseDir" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="baseDir" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d/String" />
          </type>
        </attributeProperty>
        <attributeProperty name="LocalRoute" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="localRoute" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d/String" />
          </type>
        </attributeProperty>
        <attributeProperty name="FilerPort" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="filerPort" isReadOnly="false" defaultValue="12345">
          <type>
            <externalTypeMoniker name="/fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d/Int32" />
          </type>
        </attributeProperty>
        <attributeProperty name="StatusPort" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="statusPort" isReadOnly="false" defaultValue="0">
          <type>
            <externalTypeMoniker name="/fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d/Int32" />
          </type>
        </attributeProperty>
        <attributeProperty name="HeartbeatIntervalMs" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="heartbeatIntervalMs" isReadOnly="false" defaultValue="10000">
          <type>
            <externalTypeMoniker name="/fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d/Int32" />
          </type>
        </attributeProperty>
        <attributeProperty name="MessageBus" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="messageBus" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d/String" />
          </type>
        </attributeProperty>
        <attributeProperty name="Multitenant" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="multiTenant" isReadOnly="false" defaultValue="true">
          <type>
            <externalTypeMoniker name="/fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d/Boolean" />
          </type>
        </attributeProperty>
        <attributeProperty name="MaxMemoryMB" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="maxMemoryMB" isReadOnly="false" defaultValue="2048">
          <type>
            <externalTypeMoniker name="/fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d/Int32" />
          </type>
        </attributeProperty>
        <attributeProperty name="Secure" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="secure" isReadOnly="false" defaultValue="true">
          <type>
            <externalTypeMoniker name="/fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d/Boolean" />
          </type>
        </attributeProperty>
        <attributeProperty name="EnforceUsageLimit" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="enforceUlimit" isReadOnly="false" defaultValue="true">
          <type>
            <externalTypeMoniker name="/fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d/Boolean" />
          </type>
        </attributeProperty>
        <attributeProperty name="DisableDirCleanup" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="disableDirCleanup" isReadOnly="false" defaultValue="false">
          <type>
            <externalTypeMoniker name="/fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d/Boolean" />
          </type>
        </attributeProperty>
        <attributeProperty name="UseDiskQuota" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="useDiskQuota" isReadOnly="false" defaultValue="true">
          <type>
            <externalTypeMoniker name="/fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d/Boolean" />
          </type>
        </attributeProperty>
        <attributeProperty name="MaxConcurrentStarts" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="maxConcurrentStarts" isReadOnly="false" defaultValue="3">
          <type>
            <externalTypeMoniker name="/fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d/Int32" />
          </type>
        </attributeProperty>
        <attributeProperty name="Index" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="index" isReadOnly="false" defaultValue="-1">
          <type>
            <externalTypeMoniker name="/fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d/Int32" />
          </type>
        </attributeProperty>
        <attributeProperty name="AdvertiseIntervalMs" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="advertiseIntervalMs" isReadOnly="false" defaultValue="5000">
          <type>
            <externalTypeMoniker name="/fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d/Int32" />
          </type>
        </attributeProperty>
        <attributeProperty name="Domain" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="domain" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d/String" />
          </type>
        </attributeProperty>
        <attributeProperty name="UploadThrottleBitsps" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="uploadThrottleBitsps" isReadOnly="false" documentation="The network outbound throttle limit to be enforced for the running apps. Units are in Bits Per Second.">
          <type>
            <externalTypeMoniker name="/fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d/Int64" />
          </type>
        </attributeProperty>
        <attributeProperty name="LogyardUidPath" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="logyardUidPath" isReadOnly="false" defaultValue="&quot;&quot;">
          <type>
            <externalTypeMoniker name="/fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d/String" />
          </type>
        </attributeProperty>
      </attributeProperties>
      <elementProperties>
        <elementProperty name="Stacks" isRequired="false" isKey="false" isDefaultCollection="true" xmlName="stacks" isReadOnly="false">
          <type>
            <configurationElementCollectionMoniker name="/fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d/StackCollection" />
          </type>
        </elementProperty>
        <elementProperty name="DirectoryServer" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="directoryServer" isReadOnly="false">
          <type>
            <configurationElementMoniker name="/fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d/DirectoryServerElement" />
          </type>
        </elementProperty>
        <elementProperty name="Staging" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="staging" isReadOnly="false">
          <type>
            <configurationElementMoniker name="/fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d/StagingElement" />
          </type>
        </elementProperty>
      </elementProperties>
    </configurationElement>
    <configurationElementCollection name="StackCollection" collectionType="BasicMap" xmlItemName="stack" codeGenOptions="Indexer, AddMethod, RemoveMethod, GetItemMethods">
      <itemType>
        <configurationElementMoniker name="/fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d/StackElement" />
      </itemType>
    </configurationElementCollection>
    <configurationElement name="StackElement">
      <attributeProperties>
        <attributeProperty name="Name" isRequired="true" isKey="true" isDefaultCollection="false" xmlName="name" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d/String" />
          </type>
        </attributeProperty>
      </attributeProperties>
    </configurationElement>
    <configurationElementCollection name="SupportedVersionsCollection" collectionType="BasicMap" xmlItemName="supportedVersion" codeGenOptions="Indexer, AddMethod, RemoveMethod, GetItemMethods">
      <attributeProperties>
        <attributeProperty name="DefaultVersion" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="defaultVersion" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d/String" />
          </type>
        </attributeProperty>
      </attributeProperties>
      <itemType>
        <configurationElementMoniker name="/fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d/SupportedVersionElement" />
      </itemType>
    </configurationElementCollection>
    <configurationElement name="SupportedVersionElement">
      <attributeProperties>
        <attributeProperty name="Name" isRequired="true" isKey="true" isDefaultCollection="false" xmlName="name" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d/String" />
          </type>
        </attributeProperty>
      </attributeProperties>
    </configurationElement>
    <configurationElement name="DirectoryServerElement">
      <attributeProperties>
        <attributeProperty name="FileApiPort" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="fileApiPort" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d/Int32" />
          </type>
        </attributeProperty>
        <attributeProperty name="V1Port" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="v1Port" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d/Int32" />
          </type>
        </attributeProperty>
        <attributeProperty name="V2Port" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="v2Port" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d/Int32" />
          </type>
        </attributeProperty>
        <attributeProperty name="StreamingTimeoutMS" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="streamingTimeoutMS" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d/Int32" />
          </type>
        </attributeProperty>
        <attributeProperty name="Logger" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="logger" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d/String" />
          </type>
        </attributeProperty>
      </attributeProperties>
    </configurationElement>
    <configurationElement name="StagingElement">
      <attributeProperties>
        <attributeProperty name="Enabled" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="enabled" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d/Boolean" />
          </type>
        </attributeProperty>
        <attributeProperty name="BuildpacksDirectory" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="buildpacksDirectory" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d/String" />
          </type>
        </attributeProperty>
        <attributeProperty name="StagingTimeoutMs" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="stagingTimeoutMs" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d/Int32" />
          </type>
        </attributeProperty>
        <attributeProperty name="GitExecutable" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="gitExecutable" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/fe3fc0b9-36cd-404c-8c6b-49c6d0ea824d/String" />
          </type>
        </attributeProperty>
      </attributeProperties>
    </configurationElement>
  </configurationElements>
  <propertyValidators>
    <validators />
  </propertyValidators>
</configurationSectionModel>