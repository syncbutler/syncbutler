## Steps to Embed resources: ##
  1. **Right click** on the file in the **Solution Explorer**
  1. Select **Properties**
  1. Change the **Build Action** to **Embedded Resource**

## Required References ##
```
using System.IO;
using System.Reflection;
```

## Codes to access it ##
```
Assembly assembly;
assembly = Assembly.GetExecutingAssembly();
Stream s = assembly.GetManifestResourceStream("SyncButler.MRU.SyncedFile.xslt");
```

## References ##
http://support.microsoft.com/kb/319292