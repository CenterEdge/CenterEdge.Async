def version
def pushTarget

pipeline {
    agent none

    stages {
        stage('Build') {
            agent {
                docker {
                    image 'mcr.microsoft.com/dotnet/sdk:5.0-buster-slim'
                }
            }

            environment {
                NUGET_API_KEY = credentials('myget-api-key')
                HOME = '/tmp' // Doesn't require root user
                DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
                DOTNET_CLI_TELEMETRY_OPTOUT = '1'
            }

            steps {
                script {
                    if (env.BRANCH_NAME =~ /\d+\.\d+\.\d+(-[\w\d\-]+)?/) {
                        // Tagged release
                        version = env.BRANCH_NAME
                        pushTarget = 'main'
                    } else {
                        // Pull request, build alpha version number
                        version = "0.1.0-alpha-${env.BRANCH_NAME.toLowerCase()}-${env.BUILD_NUMBER}"
                        pushTarget = 'ci'
                    }
                }

                sh "dotnet restore -s https://centeredge.myget.org/F/main/auth/${NUGET_API_KEY}/api/v3/index.json ./src/CenterEdge.Async.sln"
                sh "dotnet build -c Release /p:VERSION=${version} ./src/CenterEdge.Async.sln"
                sh "dotnet test --no-build -f net5.0 -c Release -l 'trx;LogFileName=results.trx' --collect:'XPlat Code Coverage' ./src/CenterEdge.Async.sln"
                sh "dotnet pack --no-build -c Release /p:VERSION=${version} ./src/CenterEdge.Async.sln"

                stash name: 'packages', includes: "**/*.${version}.nupkg"
            }

            post {
                always {
                    mstest testResultsFile: '**/*.trx'
                    cobertura coberturaReportFile: '**/coverage.cobertura.xml'
                }
            }
        }

        stage('Publish') {
            agent {
                docker {
                    image 'mcr.microsoft.com/dotnet/sdk:5.0-buster-slim'
                }
            }

            options {
                skipDefaultCheckout()
            }

            environment {
				NUGET_API_KEY = credentials('myget-api-key')
                HOME = '/tmp' // Doesn't require root user
                DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
                DOTNET_CLI_TELEMETRY_OPTOUT = '1'
			}

            steps {
                unstash 'packages'

                sh "dotnet nuget push **/*.${version}.nupkg -s https://centeredge.myget.org/F/${pushTarget}/api/v2/package -k ${env.NUGET_API_KEY}"
            }
        }
    }
}
