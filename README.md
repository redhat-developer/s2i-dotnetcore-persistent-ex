# .NET Core Sample App for OpenShift

This repository contains an example .NET Core application that can be deployed on OpenShift.

The example is a simple CRUD web application that persists information in a database.

The example is meant to be built and run with the [s2i-dotnetcore](https://github.com/redhat-developer/s2i-dotnetcore) builder
images. The branches of this repository correspond to versions of the s2i-dotnetcore images.

# Deploying the application

## Deploy using the OpenShift client ('oc')

```sh
# Create a new OpenShift project
$ oc new-project mydemo

# Add the database
$ oc new-app postgresql-ephemeral

# Add the .NET Core application
$ oc new-app dotnet:6.0-ubi8~https://github.com/redhat-developer/s2i-dotnetcore-persistent-ex#dotnet-6.0 --context-dir app

# Add envvars from the the postgresql secret, and database service name envvar.
$ oc set env dc/s2i-dotnetcore-persistent-ex --from=secret/postgresql -e database-service=postgresql

# Make the .NET Core application accessible externally and show the url
$ oc expose service s2i-dotnetcore-persistent-ex
$ oc get route s2i-dotnetcore-persistent-ex
```

# Copyright and License

Copyright 2021 by Red Hat, Inc.

Licensed under the Apache License, Version 2.0 (the "License"); you may not
use this package except in compliance with the License (see the `LICENSE` file
included in this distribution). You may obtain a copy of the License at

   http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
License for the specific language governing permissions and limitations under
the License.
