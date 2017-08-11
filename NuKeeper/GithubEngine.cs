﻿using System;
using System.Threading.Tasks;
using NuKeeper.Configuration;
using NuKeeper.Engine;
using NuKeeper.Files;
using NuKeeper.Git;
using NuKeeper.Github;
using NuKeeper.Logging;
using NuKeeper.NuGet.Api;

namespace NuKeeper
{
    public class GithubEngine
    {
        private readonly IGithubRepositoryDiscovery _repositoryDiscovery;
        private readonly IPackageUpdatesLookup _updatesLookup;
        private readonly IPackageUpdateSelection _updateSelection;
        private readonly IGithub _github;
        private readonly INuKeeperLogger _logger;
        private readonly IFolderFactory _folderFactory;
        private readonly string _githubToken;

        public GithubEngine(
            IGithubRepositoryDiscovery repositoryDiscovery, 
            IPackageUpdatesLookup updatesLookup, 
            IPackageUpdateSelection updateSelection, 
            IGithub github,
            INuKeeperLogger logger,
            IFolderFactory folderFactory,
            Settings settings)
        {
            _repositoryDiscovery = repositoryDiscovery;
            _updatesLookup = updatesLookup;
            _updateSelection = updateSelection;
            _github = github;
            _logger = logger;
            _folderFactory = folderFactory;
            _githubToken = settings.GithubToken;
        }

        public async Task Run()
        {
            var githubUser = await _github.GetCurrentUser();
            var repositories = await _repositoryDiscovery.GetRepositories();

            foreach (var repository in repositories)
            {
                var tempFolder = _folderFactory.UniqueTemporaryFolder();
                var git = new LibGit2SharpDriver(_logger, tempFolder, githubUser, _githubToken);

                try
                {
                    var repositoryUpdater = new RepositoryUpdater(
                        _github, git, 
                        _updatesLookup, _updateSelection, 
                        tempFolder, _logger, 
                        repository);

                    await repositoryUpdater.Run();
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed on repo {repository.RepositoryName}", ex);
                }
            }
        }
    }
}
