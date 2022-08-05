﻿using AutoFixture.Xunit2;
using FluentAssertions;
using MicroFocus.FAS.Adapters.Rest.Client.Api;
using MicroFocus.FAS.Adapters.Rest.Client.Model;
using MicroFocus.FAS.AdapterSdk.Api;
using Moq;
using Objectivity.AutoFixture.XUnit2.AutoMoq.Attributes;
using AdapterDescriptor = MicroFocus.FAS.Adapters.Rest.Client.Model.AdapterDescriptor;
using RepositorySettingDefinition = MicroFocus.FAS.Adapters.Rest.Client.Model.RepositorySettingDefinition;
using RetrieveFileListRequest = MicroFocus.FAS.AdapterSdk.Api.RetrieveFileListRequest;
using RetrieveFilesDataRequest = MicroFocus.FAS.AdapterSdk.Api.RetrieveFilesDataRequest;

namespace MicroFocus.FAS.Adapters.Rest.Tests
{
    public class RestAdapterTests
    {
        [Theory]
        [AutoMockData]
        public void CreateDescriptor_should_call_api_to_obtain_descriptor([Frozen] Mock<IAdapterApi> adapterApi,
                                                                          string adapterType,
                                                                          List<RepositorySettingDefinition> definitions,
                                                                          RestAdapter sut)
        {
            adapterApi.Setup(mock => mock.AdapterDescriptorGet(0)).Returns(new AdapterDescriptor(adapterType, definitions));

            var adapterDescriptor = sut.CreateDescriptor();

            adapterDescriptor.AdapterType.Should().Be(adapterType);
            foreach (var repositorySettingDefinition in definitions)
            {
                adapterDescriptor.SettingDefinitions.Should()
                                 .Contain(definition => definition.Name == repositorySettingDefinition.Name &&
                                                        definition.IsRequired == repositorySettingDefinition.IsRequired);
            }
        }

        [Theory]
        [AutoMockData]
        public async Task RetrieveFileListAsync_should_call_api_and_queue_results([Frozen] Mock<IAdapterApi> adapterApi,
                                                                                  [Frozen] Mock<IFileListResultsHandler> fileListResultsHandler,
                                                                                  RetrieveFileListRequest fileListRequest,
                                                                                  RetrieveFileListResponse fileListResponse,
                                                                                  RestAdapter sut)
        {
            adapterApi
                .Setup(mock =>
                           mock.RetrieveFileListPostAsync(It.Is<Client.Model.RetrieveFileListRequest>(req =>
                                                                                                          req.AdditionalFilter ==
                                                                                                          fileListRequest.AdditionalFilter &&
                                                                                                          req.RepositoryProperties.RepositoryOptions
                                                                                                             .All(opt => fileListRequest.RepositoryProperties
                                                                                                                                        .RepositoryOptions
                                                                                                                                        .GetOption(opt.Key) ==
                                                                                                                         opt.Value)),
                                                          0,
                                                          CancellationToken.None))
                .ReturnsAsync(fileListResponse);


            await sut.RetrieveFileListAsync(fileListRequest, fileListResultsHandler.Object, CancellationToken.None);

            foreach (var failureDetails in fileListResponse.Failures)
            {
                fileListResultsHandler.Verify(h => h.RegisterFailureAsync(failureDetails.ItemLocation,
                                                                          It.Is<IFailureDetails>(f => f.Message == failureDetails.Message)));
            }

            foreach (var fileListItem in fileListResponse.Items)
            {
                fileListResultsHandler.Verify(h => h.QueueItemAsync(It.Is<IItemMetadata>(i => i.Name == fileListItem.ItemMetadata.Name &&
                                                                                              i.ItemLocation == fileListItem.ItemMetadata.ItemLocation &&
                                                                                              i.Size == fileListItem.ItemMetadata.Size &&
                                                                                              i.Title == fileListItem.ItemMetadata.Title &&
                                                                                              i.AccessedTime == fileListItem.ItemMetadata.AccessedTime &&
                                                                                              i.ModifiedTime == fileListItem.ItemMetadata.ModifiedTime &&
                                                                                              i.CreatedTime == fileListItem.ItemMetadata.CreatedTime &&
                                                                                              i.Version == fileListItem.ItemMetadata._Version),
                                                                    fileListItem.PartitionHint,
                                                                    CancellationToken.None));
            }
        }

        [Theory]
        [AutoMockData]
        public async Task RetrieveFileDataAsync_should_call_api_and_queue_results([Frozen] Mock<IAdapterApi> adapterApi,
                                                                                  [Frozen] Mock<IFileDataResultsHandler> fileDataResultsHandler,
                                                                                  RetrieveFilesDataRequest fileDataRequest,
                                                                                  RetrieveFileDataResponse fileDataResponse,
                                                                                  byte[] contentBytes,
                                                                                  RestAdapter sut)
        {
            foreach (var item in fileDataResponse.Items)
            {
                item.FileContents = Convert.ToBase64String(contentBytes);
            }
            adapterApi.Setup(api =>

                                 api.RetrieveFilesDataPostAsync(It.Is<Client.Model.RetrieveFileDataRequest>(req =>
                                                                                                                       req.Items.All(item =>
                                                                                                                                         fileDataRequest.Items.Any(i =>
                                                                                                                                                                       i.ItemId == item.ItemId &&
                                                                                                                                                                       i.Metadata.Name == item.Metadata.Name &&
                                                                                                                                                                       i.Metadata.ItemLocation == item.Metadata.ItemLocation &&
                                                                                                                                                                       i.Metadata.Size == item.Metadata.Size &&
                                                                                                                                                                       i.Metadata.Title == item.Metadata.Title &&
                                                                                                                                                                       i.Metadata.AccessedTime == item.Metadata.AccessedTime &&
                                                                                                                                                                       i.Metadata.ModifiedTime == item.Metadata.ModifiedTime &&
                                                                                                                                                                       i.Metadata.CreatedTime == item.Metadata.CreatedTime &&
                                                                                                                                                                       i.Metadata.Version == item.Metadata._Version))), 0, CancellationToken.None)

                             ).ReturnsAsync(fileDataResponse);

            await sut.RetrieveFilesDataAsync(fileDataRequest, fileDataResultsHandler.Object, CancellationToken.None);

            foreach (var failureDetails in fileDataResponse.Failures)
            {
                fileDataResultsHandler.Verify(h => h.RegisterFailureAsync(failureDetails.ItemLocation,
                                                                          It.Is<IFailureDetails>(f => f.Message == failureDetails.Message)));
            }

            foreach (var fileDataItem in fileDataResponse.Items)
            {
                var bytes = Convert.FromBase64String(fileDataItem.FileContents);
                fileDataResultsHandler.Verify(h => h.QueueItemAsync(fileDataItem.ItemId, It.Is<IFileContents>(c => Enumerable.SequenceEqual(c.ContentStream().ToByteArray(), bytes)),
                                                                    It.Is<IItemMetadata>(i => i.Name == fileDataItem.Metadata.Name &&
                                                                                              i.ItemLocation == fileDataItem.Metadata.ItemLocation &&
                                                                                              i.Size == fileDataItem.Metadata.Size &&
                                                                                              i.Title == fileDataItem.Metadata.Title &&
                                                                                              i.AccessedTime == fileDataItem.Metadata.AccessedTime &&
                                                                                              i.ModifiedTime == fileDataItem.Metadata.ModifiedTime &&
                                                                                              i.CreatedTime == fileDataItem.Metadata.CreatedTime &&
                                                                                              i.Version == fileDataItem.Metadata._Version), CancellationToken.None));
            }
        }
    }
}