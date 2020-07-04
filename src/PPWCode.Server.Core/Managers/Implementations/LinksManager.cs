// Copyright 2020 by PeopleWare n.v..
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

using PPWCode.API.Core;
using PPWCode.Server.Core.Managers.Interfaces;
using PPWCode.Server.Core.RequestContext.Interfaces;
using PPWCode.Vernacular.Persistence.IV;

namespace PPWCode.Server.Core.Managers.Implementations
{
    /// <inheritdoc cref="ILinksManager{TModel,TIdentity,TDto,TContext}" />
    public abstract class LinksManager<TModel, TIdentity, TDto, TContext>
        : Manager,
          ILinksManager<TModel, TIdentity, TDto, TContext>
        where TIdentity : struct, IEquatable<TIdentity>
        where TModel : class, IPersistentObject<TIdentity>
        where TDto : class, ILinksDto<TIdentity>
        where TContext : LinksContext, new()
    {
        protected LinksManager([NotNull] IRequestContext requestContext)
        {
            RequestContext = requestContext;
        }

        [NotNull]
        public IRequestContext RequestContext { get; }

        /// <summary>
        ///     The <see cref="Route" /> together with the member <see cref="GetRouteParameters" /> is being used to calculate a
        ///     unique <see cref="Uri" /> to our resource of type <typeparamref name="TModel" />.
        /// </summary>
        [CanBeNull]
        protected abstract string Route { get; }

        /// <summary>
        ///     Name of self for entry in Links.
        /// </summary>
        protected virtual string SelfKey
            => "self";

        /// <summary>
        ///     Name of href for entry in Links.
        /// </summary>
        protected virtual string HRefKey
            => "href";

        /// <inheritdoc />
        public void Initialize(TModel model, TDto dto)
            => Initialize(model, dto, new TContext());

        /// <inheritdoc />
        public void Initialize(TModel model, TDto dto, TContext context)
        {
            Uri href = GetHref(model, context);
            if (href != null)
            {
                AddLink(dto, SelfKey, new Dictionary<string, object> { { HRefKey, href } });
                dto.HRef = href;
            }

            foreach (KeyValuePair<string, IDictionary<string, object>> additionalLink in
                GetAdditionalLinks(model, context)
                    .Where(kv => !string.IsNullOrWhiteSpace(kv.Key) && (kv.Value != null)))
            {
                AddLink(dto, additionalLink.Key, additionalLink.Value);
            }
        }

        /// <summary>
        ///     Returns all identifiers that are necessary to calculate a unique <see cref="Uri" /> to our resource of type
        ///     <typeparamref name="TModel" />.
        /// </summary>
        /// <param name="source">Model where we extract our information</param>
        /// <param name="context">Context that can be used while mapping</param>
        /// <returns>
        ///     All identifiers necessary to calculate a unique <see cref="Uri" /> to our resource of type
        ///     <typeparamref name="TModel" />.
        /// </returns>
        [CanBeNull]
        protected abstract IDictionary<string, object> GetRouteParameters([NotNull] TModel model, [NotNull] TContext context);

        /// <summary>
        ///     Add a new link to <see cref="ILinksDto{TIdentity}.Links" />.
        /// </summary>
        /// <param name="dto">Dto that contains a member <see cref="ILinksDto{TIdentity}.Links" /></param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the work.</param>
        /// <param name="key">Key of the link</param>
        /// <param name="href">Link itself</param>
        /// <remarks>A key can be added, if the key is not already exists as link and the reference is not null.</remarks>
        /// <result>If they is added, it will return true.</result>
        protected virtual bool AddLink(
            [NotNull] TDto dto,
            [NotNull] string key,
            [NotNull] IDictionary<string, object> value)
        {
            if (dto.Links == null)
            {
                dto.Links = new Dictionary<string, IDictionary<string, object>>();
            }

            if (!dto.Links.ContainsKey(key))
            {
                dto.Links.Add(key, value);
                return true;
            }

            return false;
        }

        /// <summary>
        ///     The possibility to enrich the <see cref="ILinksDto{TIdentity}.Links" /> list.
        /// </summary>
        /// <param name="source">The model</param>
        /// <param name="context">Context that can be used while mapping.</param>
        /// <returns>List of key / href pairs, to be added to our Links dictionary.</returns>
        [NotNull]
        protected virtual IEnumerable<KeyValuePair<string, IDictionary<string, object>>> GetAdditionalLinks(
            [NotNull] TModel model,
            [NotNull] TContext context)
        {
            yield break;
        }

        /// <summary>
        ///     Calculates a unique <see cref="Uri" />, for the <paramref name="model" />
        /// </summary>
        /// <param name="model">The model for which we have to calculate a unique <see cref="Uri" /></param>
        /// <param name="context">Context that can be used while mapping</param>
        /// <param name="route">Optional route, if rout is <c>null</c>, <see cref="Route" /> will be taken</param>
        /// <param name="routeParameters">
        ///     Optional route-parameters, if rout-parameters is <c>null</c>,
        ///     <see cref="GetRouteParameters" /> will be taken
        /// </param>
        /// <returns>
        ///     A unique <see cref="Uri" />, based on <paramref name="route" /> and <paramref name="routeParameters" />.
        /// </returns>
        [CanBeNull]
        protected Uri GetHref(
            [NotNull] TModel model,
            [NotNull] TContext context,
            [CanBeNull] string route = null,
            [CanBeNull] IDictionary<string, object> routeParameters = null)
        {
            routeParameters = routeParameters ?? GetRouteParameters(model, context);
            route = route ?? Route;

            if ((route != null) && (routeParameters != null))
            {
                context.AddVersionToRouteParameters(routeParameters);
                string link = RequestContext.Link(route, routeParameters);
                return link != null ? new Uri(link) : null;
            }

            return null;
        }
    }
}