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

using System.Linq;
using System.Net;
using MicroFocus.FAS.Adapters.Rest;
using MicroFocus.FAS.Adapters.Rest.Client.Api;
using MicroFocus.FAS.Adapters.Rest.Client.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
               .ConfigureServices((hostBuilder, services) =>
                                  {
                                      var configurationSection = hostBuilder.Configuration.GetSection(nameof(RestAdapterSettings));
                                      var restAdapterSettings = configurationSection.Get<RestAdapterSettings>();
                                      var apiConfiguration = new Configuration();
                                      apiConfiguration.BasePath = restAdapterSettings.BasePath;
                                      if (restAdapterSettings.Proxy != null)
                                      {
                                          apiConfiguration.Proxy = new WebProxy(restAdapterSettings.Proxy.Address,
                                                                                restAdapterSettings.Proxy.BypassOnLocal,
                                                                                restAdapterSettings.Proxy.BypassList?.ToArray());
                                          if (restAdapterSettings.Proxy.UserName != null)
                                          {
                                              apiConfiguration.Proxy.Credentials = new NetworkCredential(restAdapterSettings.Proxy.UserName,
                                                                                                         restAdapterSettings.Proxy.Password);
                                          }
                                      }
                                      services.AddSingleton<IAdapterApi>(new AdapterApi());
                                      services.Configure<RestAdapterSettings>(configurationSection);
                                      services.AddHostedService<Worker>();
                                  })
               .Build();

await host.RunAsync();
