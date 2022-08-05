/**
 * Copyright 2022 Micro Focus or one of its affiliates.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using MicroFocus.FAS.Adapters.Rest.Client.Api;
using MicroFocus.FAS.Adapters.Rest.Client.Model;
using MicroFocus.FAS.AdapterSdk.Api;
using AdapterDescriptor = MicroFocus.FAS.AdapterSdk.Api.AdapterDescriptor;
using FailureDetails = MicroFocus.FAS.AdapterSdk.Api.FailureDetails;
using ItemMetadata = MicroFocus.FAS.AdapterSdk.Api.ItemMetadata;
using RepositoryItem = MicroFocus.FAS.Adapters.Rest.Client.Model.RepositoryItem;
using RepositorySettingDefinition = MicroFocus.FAS.AdapterSdk.Api.RepositorySettingDefinition;
using RetrieveFileListRequest = MicroFocus.FAS.AdapterSdk.Api.RetrieveFileListRequest;

namespace MicroFocus.FAS.Adapters.Rest
{
    public class RestAdapter : IRepositoryAdapter
    {
        private readonly IAdapterApi _api;

        public RestAdapter(IAdapterApi api)
        {
            _api = api;
        }


        public IAdapterDescriptor CreateDescriptor()
        {
            var restDescriptor = _api.AdapterDescriptorGet();
            return new AdapterDescriptor(restDescriptor.AdapterType,
                                         restDescriptor.PropertyDefinition.Select(pd => new RepositorySettingDefinition(pd.Name,
                                                                                                                        TypeCode.String,
                                                                                                                        pd.IsRequired,
                                                                                                                        false)));
        }

        public async Task RetrieveFileListAsync(RetrieveFileListRequest request, IFileListResultsHandler handler, CancellationToken cancellationToken)
        {
            var configurationOptions = ConvertOptions(request.RepositoryProperties.ConfigurationOptions);
            var repositoryOptions = ConvertOptions(request.RepositoryProperties.RepositoryOptions);

            var data = await _api.RetrieveFileListPostAsync(new Client.Model.RetrieveFileListRequest(request.AdditionalFilter,
                                                                                                     new RepositoryProperties(configurationOptions,
                                                                                                                              repositoryOptions)),
                                                            cancellationToken: cancellationToken);
            foreach (var failureDetails in data.Failures)
            {
                await handler.RegisterFailureAsync(failureDetails.ItemLocation, new FailureDetails(failureDetails.Message));
            }

            foreach (var item in data.Items)
            {
                await handler.QueueItemAsync(ConvertMetadata(item.ItemMetadata), item.PartitionHint, cancellationToken);
            }
        }

        public async Task RetrieveFilesDataAsync(RetrieveFilesDataRequest request, IFileDataResultsHandler handler, CancellationToken cancellationToken)
        {
            var configurationOptions = ConvertOptions(request.RepositoryProperties.ConfigurationOptions);
            var repositoryOptions = ConvertOptions(request.RepositoryProperties.RepositoryOptions);
            var repositoryItems = request.Items.Select(item => new RepositoryItem(item.ItemId, ConvertMetadata(item.Metadata))).ToList();
            var data =
                await _api.RetrieveFilesDataPostAsync(new RetrieveFileDataRequest(new RepositoryProperties(configurationOptions, repositoryOptions),
                                                                                  repositoryItems),
                                                      cancellationToken: cancellationToken);

            await ProcessFailures(data.Failures, handler);

            foreach (var item in data.Items)
            {
                await handler.QueueItemAsync(item.ItemId,
                                             new FileContents(Convert.FromBase64String(item.FileContents)),
                                             ConvertMetadata(item.Metadata),
                                             cancellationToken);
            }
        }

        private static async Task ProcessFailures(IEnumerable<Client.Model.FailureDetails> failures, IFailureRegistration failureRegistration)
        {
            foreach (var failureDetails in failures)
            {
                await failureRegistration.RegisterFailureAsync(failureDetails.ItemLocation, new FailureDetails(failureDetails.Message));
            }
        }

        private static IItemMetadata ConvertMetadata(Client.Model.ItemMetadata restItemMetadata)
        {
            return new ItemMetadata(restItemMetadata.Name, restItemMetadata.ItemLocation)
                   {
                       Version = restItemMetadata._Version,
                       Size = restItemMetadata.Size,
                       Title = restItemMetadata.Title,
                       AccessedTime = Convert.ToDateTime(restItemMetadata.AccessedTime),
                       ModifiedTime = Convert.ToDateTime(restItemMetadata.ModifiedTime),
                       CreatedTime = Convert.ToDateTime(restItemMetadata.CreatedTime),
                       AdditionalMetadata =
                           restItemMetadata.AdditionalMetadata.ToDictionary(d => d.Key,
                                                                            d => (object)d.Value)
                   };
        }

        private static Client.Model.ItemMetadata ConvertMetadata(IItemMetadata itemMetadata)
        {
            return new Client.Model.ItemMetadata(itemMetadata.ItemLocation,
                                                 itemMetadata.Name,
                                                 itemMetadata.Title,
                                                 itemMetadata.Size,
                                                 itemMetadata.CreatedTime,
                                                 itemMetadata.AccessedTime,
                                                 itemMetadata.ModifiedTime.Value,
                                                 itemMetadata.Version,
                                                 itemMetadata.AdditionalMetadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString()));
        }

        private static Dictionary<string, string> ConvertOptions(IOptionsProvider optionsProvider)
        {
            var result = new Dictionary<string, string>();
            foreach (var optionName in optionsProvider.OptionNames)
            {
                var value = optionsProvider.GetOption(optionName);
                if (value == null)
                {
                    throw new InvalidOperationException($"Option {optionName} value cannot be null.");
                }
                result.Add(optionName, value);
            }

            return result;
        }
    }
}
