$currentDirectory = split-path $MyInvocation.MyCommand.Definition

# See if we have the ClientSecret available
if([string]::IsNullOrEmpty($env:SignClientSecret)){
	Write-Host "Client Secret not found, not signing packages"
	return;
}

dotnet tool install --tool-path . SignClient

# Setup Variables we need to pass into the sign client tool
$appSettings = "$currentDirectory\SignClient.json"

Write-Host "Listing *.nupkg in $Env:ArtifactDirectory"

$nupkgs = gci $Env:ArtifactDirectory\*.nupkg -recurse | Select -ExpandProperty FullName

foreach ($nupkg in $nupgks){
	Write-Host "Submitting $nupkg for signing"

	.\SignClient 'sign' -c $appSettings -i $nupkg -r $env:SignClientUser -s $env:SignClientSecret -n 'RSAKeyVaultProvider' -d 'RSAKeyVaultProvider' -u 'https://github.com/onovotny/RSAKeyVaultProvider' 

	Write-Host "Finished signing $nupkg"
}

Write-Host "Sign-package complete"