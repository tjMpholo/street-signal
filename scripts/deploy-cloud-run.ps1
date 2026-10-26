param(
 [Parameter(Mandatory=$true)][string]$RuntimeServiceAccount,
 [string]$Project='street-signal-app-8f363',
 [string]$Region='africa-south1',
 [string]$Service='street-signal-dashboard'
)
$ErrorActionPreference='Stop'
if(!(Get-Command gcloud -ErrorAction SilentlyContinue)){throw 'Install Google Cloud CLI and run gcloud auth login first.'}
function Invoke-Cloud {
 param([string[]]$Arguments)
 & gcloud @Arguments
 if($LASTEXITCODE -ne 0){throw 'Google Cloud command failed. Review the error above.'}
}
# The runtime identity needs roles/datastore.user and roles/firebaseauth.viewer.
# Billing and the Run, Cloud Build, Artifact Registry and Secret Manager APIs must be enabled.
# Create secret street-signal-firebase-api-key and grant its accessor role to the runtime identity.
# No private service account JSON is uploaded.
$taskRoot=Split-Path $PSScriptRoot -Parent
Push-Location $taskRoot
try {
 Invoke-Cloud -Arguments @('run','deploy',$Service,'--source','.',"--project=$Project","--region=$Region",'--port=8080',"--service-account=$RuntimeServiceAccount",'--allow-unauthenticated','--max-instances=1','--min-instances=0','--concurrency=20','--memory=512Mi','--cpu=1',"--set-env-vars=Firebase__ProjectId=$Project,Hosting__ManagedTls=true",'--set-secrets=Firebase__ApiKey=street-signal-firebase-api-key:latest','--quiet')
 Invoke-Cloud -Arguments @('run','services','describe',$Service,"--project=$Project","--region=$Region",'--format=value(status.url)')
} finally {Pop-Location}
