#!/bin/bash

# This script is used to test the chronicler chart using kind.
# It installs the chart and validates it starts up correctly.

# Define kind cluster name
cluster_name=chronicler-test

# Ensures script fails if something goes wrong.
set -eo pipefail

# define cleanup function
cleanup() {
    rm -fr $temp_folderx
    kind delete cluster -n ${cluster_name} >/dev/null 2>&1
}

# define debug function
debug() {
    echo -e "\nDebugging information:"
    echo -e "\nHelm status:"
    helm status chronicler -n chronicler --show-desc --show-resources

    echo -e "\nDeployment description:"
    kubectl describe deployment -n chronicler po-chronicler-deployment

    POD_NAMES=$(kubectl get pods -n chronicler -l app=po-chronicler -o jsonpath="{.items[*].metadata.name}")
    # Loop over the pods and print their logs
    for POD_NAME in $POD_NAMES
    do
        echo -e "\nLogs for $POD_NAME:"
        kubectl logs -n chronicler $POD_NAME
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
kind delete cluster -n ${cluster_name}
kind create cluster -n ${cluster_name}

# create namespace
kubectl create namespace chronicler

# install postgresql chart
helm install postgresql oci://registry-1.docker.io/bitnamicharts/postgresql --namespace chronicler

# build docker image
docker build -f src/Chronicler.Dockerfile -t ghcr.io/project-origin/chronicler:test src/

# load docker image into cluster
kind load -n ${cluster_name} docker-image ghcr.io/project-origin/chronicler:test

# generate keys
openssl genpkey -algorithm ED25519 > ${secret_filename}

# generate secret
kubectl create secret generic signing-key --from-file=my-key=${secret_filename} --namespace chronicler

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
  networkConfigurationFile: |-
    {
      "RegistryUrls": {
        "narniaRegistry": "https://registry.narnia.example.com",
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
helm install chronicler ./chart --values ${values_filename} --namespace chronicler --wait

echo "Test completed successfully ✅"
