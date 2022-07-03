#!/bin/bash
# Convenience script for build and run every service
set -eo pipefail

cd TodoListService
docker build -t todo-list:before-otel .
cd ../TodoItemService
docker build -t todo-item:before-otel .
cd ..
kubectl create namespace dotnet-otel-elastic
kubectl config set-context --current --namespace=dotnet-otel-elastic
kubectl apply -f ./infra/mssql.yaml
kubectl apply -f ./TodoItemService/deploy-k8s.yaml
kubectl apply -f ./TodoListService/deploy-k8s.yaml
