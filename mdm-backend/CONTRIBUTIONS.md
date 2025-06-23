# Developer Guide for contribution

This simple guide will be use to guide easy development

## Editor

1. VS Code
2. Visual Studio Community Version
3. [smtp4dev](https://github.com/rnwood/smtp4dev) for local stmp
   ```
   dotnet tool install -g Rnwood.Smtp4dev --version "3.1.0-*"
   smtp4dev --server.urls "http://0.0.0.0:5005/"
   ```
   Now open http://0.0.0.0:5005/ to see the message

### Checking code coverage with VS Code

1. Install all the workspace recommended plugins

   ![Workspace recommendations](https://api.monosnap.com/file/download?id=iXxkizgit1ZaFz64st5vnfPnOQnYAr)

1. Generate code coverage report which will be read by [Coverage Gutters](https://marketplace.visualstudio.com/items?itemName=ryanluker.vscode-coverage-gutters)

1. Now, you can see the code coverage in the visual studio code editor as

   ![Coverage Gutters Highlight](https://api.monosnap.com/file/download?id=GzKDLwyNHpT5GaDAYNeiGMNLptEcUQ)

1. Alternative you can generate the detail report as mentioned on [To generate code coverage report](./readme.md#run-tests)

   ![Code Coverage HTML report](https://api.monosnap.com/file/download?id=bKLdH24u1WLW3ozDkhEmRcVBntxyEk)
