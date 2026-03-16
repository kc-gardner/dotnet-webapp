# .NET 8 Container Demo

A containerized ASP.NET Core web application built on Red Hat UBI images,
designed to demonstrate how .NET workloads run on Podman and OpenShift.

## Purpose

The application intentionally displays configuration values and user claims on
the page. This is a demo tool. It exists to make the platform's capabilities
visible, not to serve as a production application pattern.

This repo demonstrates:

- Container build pipeline: Multi-stage Containerfile using UBI 8 .NET SDK
  and runtime images.
- Externalized configuration: Environment variables and Kubernetes
  ConfigMaps/Secrets replace `appsettings.json` at runtime with no code changes.
- Entra ID authentication: OpenID Connect integration with Microsoft Entra ID,
  returning AD group memberships for role-based access.
- GitOps deployment files will be includes: Kustomize base/overlay structure 
  for deploying across environments with ArgoCD.