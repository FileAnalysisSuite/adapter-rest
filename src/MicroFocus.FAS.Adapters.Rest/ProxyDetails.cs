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
using System.Diagnostics.CodeAnalysis;

namespace MicroFocus.FAS.Adapters.Rest
{
    [SuppressMessage("ReSharper",
                     "ClassNeverInstantiated.Global",
                     Justification = "This is a configuration class instantiated by the configuration system")]
    public class ProxyDetails
    {
        public string Address { get; set; }

        public bool BypassOnLocal { get; set; }

        public IEnumerable<string>? BypassList { get; set; }

        public string? UserName { get; set; }

        public string? Password { get; set; }
    }
}
