#!/bin/bash
export OrganisationUUID=29189838:d2db66a3-351a-4f8a-abe1-990ac9de4de4,29189714:f1592d93-b77c-4d06-991b-bcc16fcb1aaf
export ClientCertPath=/home/brian/Dropbox/FOCES/sp-test.pfx
export ClientCertPassword=Test1234
export LogRequestResponse=false
export DisableRevocationCheck=true
export DBConnectionString=
export EnableScheduler=false
export Environment=TEST
export UseSSL=false
export LogLevel=DEBUG
export Municipality=29189838

dotnet run
