param(
    [Parameter(Mandatory = $true)]
    [string]$InputPath,

    [string]$OutputPath
)

if (-not (Test-Path -LiteralPath $InputPath)) {
    throw "Input file '$InputPath' was not found."
}

$schemaNamespace = 'http://www.w3.org/2001/XMLSchema'
$doc = New-Object System.Xml.XmlDocument
$doc.PreserveWhitespace = $true
$doc.Load($InputPath)

function Set-XsdPrefix {
    param(
        [System.Xml.XmlNode]$Node,
        [string]$Namespace,
        [string]$Prefix
    )

    if ($null -eq $Node) {
        return
    }

    if ($Node.NamespaceURI -eq $Namespace) {
        $Node.Prefix = $Prefix
    }

    if ($Node.Attributes) {
        foreach ($attr in $Node.Attributes) {
            if ($attr.NamespaceURI -eq $Namespace -and $attr.Prefix -ne 'xmlns') {
                $attr.Prefix = $Prefix
            }
        }
    }

    foreach ($child in $Node.ChildNodes) {
        Set-XsdPrefix -Node $child -Namespace $Namespace -Prefix $Prefix
    }
}

Set-XsdPrefix -Node $doc.DocumentElement -Namespace $schemaNamespace -Prefix 'xs'

$root = $doc.DocumentElement
if ($null -eq $root) {
    throw 'The provided XML does not have a document element.'
}

if (-not $root.HasAttribute('xmlns:xs')) {
    $root.SetAttribute('xmlns:xs', $schemaNamespace)
}

if ($root.HasAttribute('xmlns') -and $root.GetAttribute('xmlns') -eq $schemaNamespace) {
    $root.RemoveAttribute('xmlns')
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = $InputPath
}

$settings = New-Object System.Xml.XmlWriterSettings
$settings.OmitXmlDeclaration = $false
$settings.Indent = $true
$settings.NewLineHandling = [System.Xml.NewLineHandling]::Replace

$writer = [System.Xml.XmlWriter]::Create($OutputPath, $settings)
try {
    $doc.WriteTo($writer)
}
finally {
    $writer.Close()
}
