#!/bin/bash
# Convenience script for build and run every service
set -eo pipefail

printf 'Building services with docker\n'
cd TodoListService
docker build -t todo-list:latest .
cd ../TodoItemService
docker build -t todo-item:latest .
cd ..

printf 'Setting up namespace\n'
if kubectl get ns dotnet-otel-elastic
then
  printf 'Namespace already exists\n'
else
  kubectl create namespace dotnet-otel-elastic
fi
kubectl config set-context --current --namespace=dotnet-otel-elastic

printf 'Setting up mssql secret\n'
if kubectl get secret mssql
then
  printf 'mssql secret already exists\n'
else
  mssql_pwd="PW@$(openssl rand -hex 20)"
  kubectl create secret generic mssql --from-literal="SA_PASSWORD=$mssql_pwd"
fi

kubectl apply -f ./infra/mssql.yaml
kubectl apply -f ./infra/elastic-stack.yaml
kubectl apply -f ./TodoItemService/deploy.yaml
kubectl apply -f ./TodoListService/deploy.yaml
