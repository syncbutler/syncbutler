<?xml version="1.0"?>

<xsl:stylesheet version="1.0"
xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

  <xsl:template match="/">
    <html>
      <body>
        <h2>
          <xsl:value-of select="MRUList/ComputerName" />
        </h2>
        <table border="1">
          <tr>
            <th>Orginal Path</th>
            <th>Copied to</th>
          </tr>
          <xsl:for-each select="MRUList/MRUs/MRU">
            <tr>
              <td>
                <xsl:value-of select="OriginalPath" />
              </td>
              <td>
                <xsl:value-of select="CopiedTo" />
              </td>
            </tr>
          </xsl:for-each>
        </table>
      </body>
    </html>
  </xsl:template>

</xsl:stylesheet>