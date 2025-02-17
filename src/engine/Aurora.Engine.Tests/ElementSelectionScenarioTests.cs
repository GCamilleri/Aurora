﻿using Aurora.Engine.Elements;
using Aurora.Engine.Elements.Abstractions;
using Aurora.Engine.Elements.Rules;
using Aurora.Engine.Generation;
using Aurora.Engine.Hosting;
using Aurora.Engine.Scenarios.ElementSelection;
using Aurora.Engine.Scenarios.ElementSelection.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Aurora.Engine.Tests;

[TestClass]
public class ElementSelectionScenarioTests
{
    private IEngineHost engine = null!;
    private readonly Mock<IElementSelectionPresenterFactory> presenterFactoryMock = new();
    private readonly Mock<IElementSelectionPresenter> presenterMock = new();
    private readonly Mock<IElementAggregateRegistrationManager> registrationMock = new();
    private readonly Mock<IElementDataProvider> dataProviderMock = new();

    private readonly ElementBuilder elementBuilder = new();
    private readonly List<IElement> languages = new();

    [TestInitialize]
    public void Setup()
    {
        // add some languages
        foreach (var language in new[] { "Common", "Undercommon", "Elvish", "Druidic" })
        {
            languages.Add(elementBuilder.Compose(element =>
            {
                element.Identifier = $"ID_{languages.Count + 1}";
                element.Name = language;
                element.ElementType = "Language";
            }));
        }

        // setup the data provider to return a list of languages for this test
        dataProviderMock.Setup(x => x.GetElements(It.IsAny<Func<IElement, bool>>()))
            .Returns(languages);

        // setup the factory to return a presenter mock on which we can verify
        presenterFactoryMock.Setup(x => x.CreatePresenter(It.IsAny<Action<PresenterConfiguration>>()))
            .Returns(presenterMock.Object);

        presenterMock.Setup(x => x.UpdateHeader(It.IsAny<string>()))
            .Callback<string>(x => { System.Diagnostics.Debug.WriteLine(x); });

        engine = EngineHostBuilder.CreateDefaultBuilder()
            .ConfigureScenarioDefaults()
            .ConfigureCharacterGeneration()
            .ConfigureServices(services =>
            {
                services.AddSingleton(dataProviderMock.Object); // infrastructure
                services.AddSingleton(presenterFactoryMock.Object); // presentation
                services.AddSingleton(registrationMock.Object); // mock the GenerationManager we are not testing it here, we just want to verify it was called as expected
            })
            .Build();
    }

    [TestCleanup]
    public void Teardown()
    {
        engine.Dispose();
    }

    [TestMethod]
    public void ElementSelectionHandlerManager_ShouldCreateHandler()
    {
        // arrange
        var manager = engine.Services.GetRequiredService<IElementSelectionHandlerManager>();
        var selectionRule = new SelectionRule("Language");

        // act
        var handler = manager.Create(selectionRule).FirstOrDefault();

        // assert
        Assert.IsNotNull(handler);
        Assert.IsNotNull(handler.UniqueIdentifier);
        Assert.AreNotEqual(Guid.Empty, handler.UniqueIdentifier);

        presenterFactoryMock.Verify(x => x.CreatePresenter(It.IsAny<Action<PresenterConfiguration>>()), Times.Once, "Expected a presenter for the handler to be created when the handler is created.");
        dataProviderMock.Verify(x => x.GetElements(It.IsAny<Func<IElement, bool>>()), Times.Never, "Expected the creation of the handler to not start getting data until it is initialized.");
    }

    [TestMethod]
    public void ElementSelectionHandlerManager_ShouldRemoveExistingHandler()
    {
        // arrange
        var manager = engine.Services.GetRequiredService<IElementSelectionHandlerManager>();
        var selectionRule = new SelectionRule("Language");

        // act
        var handler = manager.Create(selectionRule).FirstOrDefault();
        var isRemoved = manager.Remove(selectionRule);

        // assert
        Assert.IsTrue(isRemoved);

        presenterFactoryMock.Verify(x => x.CreatePresenter(It.IsAny<Action<PresenterConfiguration>>()), Times.Once, "Expected a presenter for the handler to be created when the handler is created.");
        dataProviderMock.Verify(x => x.GetElements(It.IsAny<Func<IElement, bool>>()), Times.Never, "Expected the creation of the handler to not start getting data until it is initialized.");
    }

    [TestMethod]
    public void ElementSelectionHandlerManager_ShouldReturnFalse_WhenRemovingNotExistingHandlers()
    {
        // arrange
        var manager = engine.Services.GetRequiredService<IElementSelectionHandlerManager>();
        var selectionRule = new SelectionRule("Language");

        // act
        var isRemoved = manager.Remove(selectionRule);

        // assert
        Assert.IsFalse(isRemoved);
    }

    [TestMethod]
    public void ElementSelectionHandler_ShouldGetElementsFromDataProvider_WhenInitialized()
    {
        // arrange
        var manager = engine.Services.GetRequiredService<IElementSelectionHandlerManager>();
        var selectionRule = new SelectionRule("Language");
        var handlers = manager.Create(selectionRule);

        // act
        handlers.ForEach(handler => _ = handler.Initialize());

        // assert
        dataProviderMock.Verify(x => x.GetElements(It.IsAny<Func<IElement, bool>>()), Times.Once, "Expected the handler get the elements from the data provider.");
    }

    [TestMethod]
    public void ElementSelectionHandler_ShouldUpdateHeaderOnPresenter_WhenInitialized()
    {
        // arrange
        var manager = engine.Services.GetRequiredService<IElementSelectionHandlerManager>();
        var selectionRule = new SelectionRule("Language");
        var handlers = manager.Create(selectionRule);

        // act
        handlers.ForEach(handler => _ = handler.Initialize());

        // assert
        presenterMock.Verify(x => x.UpdateHeader(It.IsAny<string>()), Times.Once, "Expected the header to be updated when initializing the selection handler.");
    }

    [TestMethod]
    public void ElementSelectionHandler_ShouldUpdateSelectionOptionsOnPresenter_WhenInitialized()
    {
        // arrange
        var presenterOptions = new List<ISelectionOption>();
        presenterMock.Setup(x => x.UpdateSelectionOptions(It.IsAny<IEnumerable<ISelectionOption>>()))
            .Callback<IEnumerable<ISelectionOption>>(options => { presenterOptions.AddRange(options); });

        var manager = engine.Services.GetRequiredService<IElementSelectionHandlerManager>();
        var selectionRule = new SelectionRule("Language");
        var handlers = manager.Create(selectionRule);

        // act
        handlers.ForEach(handler => _ = handler.Initialize());

        // assert
        presenterMock.Verify(x => x.UpdateSelectionOptions(It.IsAny<IEnumerable<ISelectionOption>>()), Times.Once, "Expected the selection options to be updated when initializing the selection handler.");
        Assert.AreEqual(languages.Count, presenterOptions.Count, "Expected the selection options to be the same as the number of provided language elements.");
    }

    [TestMethod]
    public void ElementSelectionHandler_ShouldRegisterElementAggregate_WhenSelectionOptionPicked()
    {
        // arrange
        var presenterOptions = new List<ISelectionOption>();
        presenterMock.Setup(x => x.UpdateSelectionOptions(It.IsAny<IEnumerable<ISelectionOption>>()))
            .Callback<IEnumerable<ISelectionOption>>(options => { presenterOptions.AddRange(options); });

        ElementAggregate? associatedAggregate = null;
        registrationMock.Setup(x => x.Register(It.IsAny<ElementAggregate>()))
            .Callback<ElementAggregate>(x => associatedAggregate = x);

        var manager = engine.Services.GetRequiredService<IElementSelectionHandlerManager>();
        var selectionRule = new SelectionRule("Language");
        var handlers = manager.Create(selectionRule);
        handlers.ForEach(handler => _ = handler.Initialize());
        var option = presenterOptions.First(); // lets register the first option

        // act        
        // (in the presentation layer you have access to the interactor, not the handler directly (you can't initialize handler from the presentation layer)
        handlers.ForEach(handler =>
        {
            var interactor = (IElementSelectionInteractor)handler;
            _ = interactor.Register(option.Identifier);
        });

        // assert
        Assert.IsNotNull(associatedAggregate);
        registrationMock.Verify(x => x.Register(associatedAggregate), Times.Once, "Expected the handler to register the selected item.");
    }

    [TestMethod]
    public async Task ElementSelectionHandler_ShouldUnregisterElementAggregate_WhenUnregisterRequested()
    {
        // arrange
        var presenterOptions = new List<ISelectionOption>();
        presenterMock.Setup(x => x.UpdateSelectionOptions(It.IsAny<IEnumerable<ISelectionOption>>()))
            .Callback<IEnumerable<ISelectionOption>>(options => { presenterOptions.AddRange(options); });

        ElementAggregate? associatedAggregate = null;
        registrationMock.Setup(x => x.Register(It.IsAny<ElementAggregate>()))
            .Callback<ElementAggregate>(x => associatedAggregate = x);

        var manager = engine.Services.GetRequiredService<IElementSelectionHandlerManager>();
        var selectionRule = new SelectionRule("Language");
        var handlers = manager.Create(selectionRule);
        handlers.ForEach(handler => _ = handler.Initialize());
        var option = presenterOptions.First(); // lets register the first option
        var interactor = (IElementSelectionInteractor)(handlers.First());
        await interactor.Register(option.Identifier);

        // act        
        var result = await interactor.Unregister();

        // assert
        Assert.IsNotNull(associatedAggregate);
        registrationMock.Verify(x => x.Unregister(associatedAggregate), Times.Once, "Expected the handler to unregister the selected item.");
    }

    [TestMethod]
    public void ElementSelectionHandlerManager_ShouldCreateMultipleHandlers()
    {
        // arrange
        var manager = engine.Services.GetRequiredService<IElementSelectionHandlerManager>();
        var selectionRule = new SelectionRule("Language");

        // act
        var handlers = manager.Create(selectionRule);

        // assert
        Assert.IsNotNull(handlers);
        Assert.AreEqual(1, handlers.Count(), "Expected one handler in the returned collection.");

        presenterFactoryMock.Verify(x => x.CreatePresenter(It.IsAny<Action<PresenterConfiguration>>()), Times.Once, "Expected a presenter for the handler to be created when the handler is created.");
        dataProviderMock.Verify(x => x.GetElements(It.IsAny<Func<IElement, bool>>()), Times.Never, "Expected the creation of the handler to not start getting data until it is initialized.");
    }
}