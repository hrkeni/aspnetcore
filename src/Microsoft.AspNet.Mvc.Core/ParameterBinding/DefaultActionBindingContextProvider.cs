// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Routing;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultActionBindingContextProvider : IActionBindingContextProvider
    {
        private readonly IModelMetadataProvider _modelMetadataProvider;
        private readonly IEnumerable<IModelBinder> _modelBinders;
        private readonly IEnumerable<IValueProviderFactory> _valueProviderFactories;
        private readonly IInputFormatterProvider _inputFormatterProvider;
        private readonly IEnumerable<IModelValidatorProvider> _validatorProviders;

        private Tuple<ActionContext, ActionBindingContext> _bindingContext;

        public DefaultActionBindingContextProvider(IModelMetadataProvider modelMetadataProvider,
                                                   IEnumerable<IModelBinder> modelBinders,
                                                   IEnumerable<IValueProviderFactory> valueProviderFactories,
                                                   IInputFormatterProvider inputFormatterProvider,
                                                   IEnumerable<IModelValidatorProvider> validatorProviders)
        {
            _modelMetadataProvider = modelMetadataProvider;
            _modelBinders = modelBinders.OrderBy(
                binder => binder.GetType() == typeof(ComplexModelDtoModelBinder) ? 1 : 0).ToArray();
            _valueProviderFactories = valueProviderFactories;
            _inputFormatterProvider = inputFormatterProvider;
            _validatorProviders = validatorProviders;
        }

        public Task<ActionBindingContext> GetActionBindingContextAsync(ActionContext actionContext)
        {
            if (_bindingContext != null)
            {
                if (actionContext == _bindingContext.Item1)
                {
                    return Task.FromResult(_bindingContext.Item2);
                }
            }

            var factoryContext = new ValueProviderFactoryContext(
                                    actionContext.HttpContext,
                                    actionContext.RouteData.Values);

            var valueProviders = _valueProviderFactories.Select(factory => factory.GetValueProvider(factoryContext))
                                                        .Where(vp => vp != null);

            var context = new ActionBindingContext(
                actionContext,
                _modelMetadataProvider,
                new CompositeModelBinder(_modelBinders),
                new CompositeValueProvider(valueProviders),
                _inputFormatterProvider,
                _validatorProviders);

            _bindingContext = new Tuple<ActionContext, ActionBindingContext>(actionContext, context);

            return Task.FromResult(context);
        }
    }
}
