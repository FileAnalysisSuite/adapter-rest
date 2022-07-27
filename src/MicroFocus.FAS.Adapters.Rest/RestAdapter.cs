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
using RepositorySettingDefinition = MicroFocus.FAS.AdapterSdk.Api.RepositorySettingDefinition;
using RetrieveFileListRequest = MicroFocus.FAS.AdapterSdk.Api.RetrieveFileListRequest;

namespace MicroFocus.FAS.Adapters.Rest
{
    internal class RestAdapter : IRepositoryAdapter
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

        public async Task RetrieveFileListAsync(
            RetrieveFileListRequest request,
            IFileListResultsHandler handler,
            CancellationToken cancellationToken)
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
        }

        public Task RetrieveFilesDataAsync(
            RetrieveFilesDataRequest request,
            IFileDataResultsHandler handler,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private static Dictionary<string, string> ConvertOptions(IOptionsProvider optionsProvider)
        {
            var result = new Dictionary<string, string>();
            foreach (var optionName in optionsProvider.OptionNames)
            {
                result.Add(optionName, optionsProvider.GetOption(optionName));
            }

            return result;
        }
    }
}
