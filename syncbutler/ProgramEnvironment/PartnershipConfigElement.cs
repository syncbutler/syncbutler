using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;

namespace SyncButler.ProgramEnvironment
{
    public class PartnershipConfigElement : ConfigurationElement
    {
        /// <summary>
        /// A default constructor for the class
        /// </summary>
        public PartnershipConfigElement()
        {
        }

        /// <summary>
        /// Generic constructor to create a config element using a details of Partnership object
        /// </summary>
        /// <param name="leftPath">Path to one of the folder in the partnership</param>
        /// <param name="rightPath">Path to another folder in the partnership</param>
        public PartnershipConfigElement(string friendlyName, string leftPath, string rightPath)
        {
            LeftPath = leftPath;
            RightPath = rightPath;
            FriendlyName = friendlyName;
        }

        /// <summary>
        /// This is the friendly name given by the user during partnership creation
        /// </summary>
        [ConfigurationProperty("friendlyName")]
        public string FriendlyName
        {
            get
            {
                return (string)this["friendlyName"];
            }
            set
            {
                this["friendlyName"] = value;
            }
        }

        /// <summary>
        /// This stores the path to one of the folder in the partnership in string format
        /// </summary>
        [ConfigurationProperty("rightPath")]
        public string RightPath
        {
            get
            {
                return (string)this["rightPath"];
            }
            set
            {
                this["rightPath"] = value;
            }
        }

        /// <summary>
        /// This stores the path to one (another) of the folder in the partnership in string format
        /// </summary>
        [ConfigurationProperty("leftPath")]
        public string LeftPath
        {
            get
            {
                return (string)this["leftPath"];
            }
            set
            {
                this["leftPath"] = value;
            }
        }
    }
}
/*
/// <summary>
/// This attribute contains the details of one partnerships that
/// exist for this system
/// </summary>
[ConfigurationProperty("partnership", IsRequired = true)]
public Partnership Partnership
{
    get
    {
        return (Partnership)this["partnership"];
    }
    set
    {
        this["partnership"] = value;
    }
}
 * 
[ConfigurationProperty("leftUniqueIdent", IsRequired = true)]
public String LeftUniqueIdent
{
    get
    {
        return (String)this["leftUniqueIdent"];
    }
    set
    {
        this["leftUniqueIdent"] = value;
    }
}

[ConfigurationProperty("rigthUniqueIdent", IsRequired = true)]
public String RigthUniqueIdent
{
    get
    {
        return (String) this["rigthUniqueIdent"];
    }
    set
    {
        this["rigthUniqueIdent"] = value;
    }
}

[ConfigurationProperty("rightPath", DefaultValue = "\\")]
public String RightPath
{
    get
    {
        return (String) this["rightPath"];
    }
    set
    {
        this["rightPath"] = value;
    }
}

[ConfigurationProperty("exclusionList")]
public List<String> ExclusionList
{
    get
    {
        return (List<String>) this["exclusionList"];
    }
    set
    {
        this["exclusionList"] = value;
    }
}

[ConfigurationProperty("inclusionList")]
public List<String> InclusionList
{
    get
    {
        return (List<String>) this["inclusionList"];
    }
    set
    {
        this["inclusionList"] = value;
    }
}

[ConfigurationProperty("ignoredList")]
public List<String> IgnoredList
{
    get
    {
        return (List<String>) this["ignoredList"];
    }
    set
    {
        this["ignoredList"] = value;
    }
}
*/
