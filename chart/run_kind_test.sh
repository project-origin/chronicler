#!/bin/bash

# This script is used to test the chronicler chart using kind.
# It installs the chart and validates it starts up correctly.

# Define kind cluster name
cluster_name=chronicler-test
namespace=chronicler

# Ensures script fails if something goes wrong.
set -eo pipefail

# define cleanup function
cleanup() {
    rm -fr $temp_folderx
    kind delete cluster --name ${cluster_name} >/dev/null 2>&1
}

# define debug function
debug() {
    echo -e "\nDebugging information:"
    echo -e "\nHelm status:"
    helm status chronicler --namespace ${namespace} --show-desc --show-resources

    echo -e "\nDeployment description:"
    kubectl describe deployment --namespace ${namespace} po-chronicler-deployment

    POD_NAMES=$(kubectl get pods --namespace ${namespace} -l app=po-chronicler -o jsonpath="{.items[*].metadata.name}")
    # Loop over the pods and print their logs
    for POD_NAME in $POD_NAMES
    do
        echo -e "\nLogs for $POD_NAME:"
        kubectl logs --namespace ${namespace} $POD_NAME
    done
}

# trap cleanup function on script exit
trap 'cleanup' 0
trap 'debug; cleanup' ERR

# define variables
temp_folder=$(mktemp -d)
values_filename=${temp_folder}/values.yaml
secret_filename=${temp_folder}/secret.yaml

# create kind cluster
kind delete cluster --name ${cluster_name}
kind create cluster --name ${cluster_name} &

# build docker image
make build-container &

# wait for cluster and container to be ready
wait

# create namespace
kubectl create namespace ${namespace}

# install postgresql chart
helm install postgresql oci://registry-1.docker.io/bitnamicharts/postgresql --namespace ${namespace}

# load docker image into cluster
kind load --name ${cluster_name} docker-image ghcr.io/project-origin/chronicler:test

# generate keys
openssl genpkey -algorithm ED25519 > ${secret_filename}

# generate secret
kubectl create secret generic signing-key --from-file=my-key=${secret_filename} --namespace ${namespace}

# generate values.yaml file
cat << EOF > "${values_filename}"
image:
  tag: test
replicaCount: 1
config:
  signingKeySecret:
    name: signing-key
    key: my-key
  gridAreas:
    - narnia
networkConfig:
  json: |-
    {
      "Registries": {
        "narniaRegistry": {
          "Url": "https://registry.narnia.example.com"
        }
      }
    }
postgresql:
  host: postgresql
  database: postgres
  username: postgres
  password:
    secretRef:
      name: postgresql
      key: postgres-password

EOF

# install chronicler chart
helm install chronicler ./chart --values ${values_filename} --namespace ${namespace} --wait --timeout 1m

# verify deployment is ready
deployments_status=$(kubectl get deployments --namespace ${namespace} --no-headers | awk '$3 != $4 {print "Deployment " $1 " is not ready"}')

# Print the results to stderr if there are any issues
if [ -n "$deployments_status" ]; then
    echo "$deployments_status" 1>&2
    echo "Test failed ❌"
else
    echo "Test completed successfully ✅"
fi
