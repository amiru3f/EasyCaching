﻿namespace EasyCaching.UnitTests
{
    using EasyCaching.Memcached;
    using Enyim.Caching;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Threading.Tasks;
    using Xunit;

    public class MemcachedProviderTest : BaseCachingProviderTest
    {
        private IMemcachedClient _client;

        public MemcachedProviderTest()
        {            
            IServiceCollection services = new ServiceCollection();
            services.AddEnyimMemcached(options => options.AddServer("127.0.0.1", 11211));
            services.AddLogging();
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            _client = serviceProvider.GetService<IMemcachedClient>();

            _provider = new DefaultMemcachedCachingProvider(_client);
            _defaultTs = TimeSpan.FromSeconds(50);
        }

        [Fact]
        public void Set_Value_And_Get_Cached_Value_Should_Succeed()
        {
            var cacheKey = Guid.NewGuid().ToString();
            var cacheValue = "value";
            _provider.Set(cacheKey, cacheValue, _defaultTs);

            var val = _provider.Get<string>(cacheKey, null, _defaultTs);
            Assert.NotNull(val);
            Assert.Equal(cacheValue, val.Value);
        }

        [Fact]
        public async Task Set_Value_And_Get_Cached_Value_Async_Should_Succeed()
        {
            var cacheKey = Guid.NewGuid().ToString();
            var cacheValue = "value";
            await _provider.SetAsync(cacheKey, cacheValue, _defaultTs);

            var val = await _provider.GetAsync<string>(cacheKey, null, _defaultTs);
            Assert.NotNull(val);
            Assert.Equal(cacheValue, val.Value);
        }

        [Fact]
        public void Get_Not_Cached_Value_Should_Call_Retriever_And_Return_Value()
        {
            var cacheKey = Guid.NewGuid().ToString();
            var func = Create_Fake_Retriever_Return_String();

            var res = _provider.Get(cacheKey, func, _defaultTs);

            Assert.Equal("123", res.Value);
        }

        [Fact]
        public async Task Get_Not_Cached_Value_Async_Should_Call_Retriever_And_Return_Value()
        {
            var cacheKey = Guid.NewGuid().ToString();
            var func = Create_Fake_Retriever_Return_String_Async();

            var res = await _provider.GetAsync(cacheKey, func, _defaultTs);

            Assert.Equal("123", res.Value);
        }

        [Fact]
        public void Get_Not_Cached_Value_Without_Retriever_Should_Return_Default_Value()
        {
            var cacheKey = Guid.NewGuid().ToString();

            var res = _provider.Get<string>(cacheKey);

            Assert.Equal(default(string), res.Value);
        }

        [Fact]
        public async Task Get_Not_Cached_Value_Without_Retriever_Async_Should_Return_Default_Value()
        {
            var cacheKey = Guid.NewGuid().ToString();

            var res = await _provider.GetAsync<string>(cacheKey);

            Assert.Equal(default(string), res.Value);
        }

        [Fact]
        public void Get_Cached_Value_Without_Retriever_Should_Return_Default_Value()
        {
            var cacheKey = Guid.NewGuid().ToString();

            _provider.Set(cacheKey,"123",_defaultTs);

            var res = _provider.Get<string>(cacheKey);

            Assert.Equal("123", res.Value);
        }

        [Fact]
        public async Task Get_Cached_Value_Without_Retriever_Async_Should_Return_Default_Value()
        {
            var cacheKey = Guid.NewGuid().ToString();

            await _provider.SetAsync(cacheKey, "123", _defaultTs);

            var res = await _provider.GetAsync<string>(cacheKey);

            Assert.Equal("123", res.Value);
        }

        [Fact]
        public void Remove_Cached_Value_Should_Succeed()
        {
            var cacheKey = Guid.NewGuid().ToString();
            var cacheValue = "value";
            _provider.Set(cacheKey, cacheValue, _defaultTs);
            var valBeforeRemove = _provider.Get<string>(cacheKey, null, _defaultTs);
            Assert.NotNull(valBeforeRemove);

            _provider.Remove(cacheKey);
            var valAfterRemove = _provider.Get(cacheKey, () => "123", _defaultTs);
            Assert.Equal("123", valAfterRemove.Value);
        }      

        [Fact]
        public async Task Remove_Cached_Value_Async_Should_Succeed()
        {
            var cacheKey = Guid.NewGuid().ToString();
            var cacheValue = "value";
            await _provider.SetAsync(cacheKey, cacheValue, _defaultTs);
            var valBeforeRemove = await _provider.GetAsync<string>(cacheKey, null, _defaultTs);
            Assert.NotNull(valBeforeRemove);

            await _provider.RemoveAsync(cacheKey);
            var valAfterRemove = await _provider.GetAsync(cacheKey,async () => await Task.FromResult("123"), _defaultTs);
            Assert.Equal("123", valAfterRemove.Value);
        }  

        [Fact]
        public void Refresh_Should_Succeed()
        {
            var cacheKey = Guid.NewGuid().ToString();
            var cacheValue = "value";
            _provider.Set(cacheKey, cacheValue, _defaultTs);

            var tmp = _provider.Get<string>(cacheKey);
            Assert.Equal("value", tmp.Value);

            _provider.Refresh(cacheKey, "NewValue", _defaultTs);

            var act = _provider.Get<string>(cacheKey);

            Assert.Equal("NewValue", act.Value);
        }

        [Fact]
        public async Task Refresh_Async_Should_Succeed()
        {
            var cacheKey = Guid.NewGuid().ToString();
            var cacheValue = "value";
            await _provider.SetAsync(cacheKey, cacheValue, _defaultTs);

            var tmp = await _provider.GetAsync<string>(cacheKey);
            Assert.Equal("value", tmp.Value);

            await _provider.RefreshAsync(cacheKey, "NewValue", _defaultTs);

            var act = await _provider.GetAsync<string>(cacheKey);

            Assert.Equal("NewValue", act.Value);
        }

        [Fact]
        public void RemoveByPrefix_Should_Succeed()
        {
            string prefixKey = "demo";
            string prefixValue = "abc";

            _provider.Set(prefixKey, prefixValue, TimeSpan.FromSeconds(120));

            SetCacheItem("1", "1", prefixKey);
            SetCacheItem("2", "2", prefixKey);
            SetCacheItem("3", "3", prefixKey);
            SetCacheItem("4", "4", prefixKey);
            SetCacheItem("4", "4", "xxx");

            _provider.RemoveByPrefix(prefixKey);

            GetCacheItem("1", prefixKey);
            GetCacheItem("2", prefixKey);
            GetCacheItem("3", prefixKey);
            GetCacheItem("4", prefixKey);

            var pre = _provider.Get<string>("xxx");
            var cacheKey = string.Concat(pre, "4");
            var val = _provider.Get<string>(cacheKey);
            Assert.True(val.HasValue);

            var afterPrefixValue = _provider.Get<string>(prefixKey);
            Assert.NotEqual(prefixValue, afterPrefixValue.Value);
        }

        [Fact]
        public async Task RemoveByPrefix_Async_Should_Succeed()
        {
            string prefixKey = "demo";
            string prefixValue = "abc";

            _provider.Set("demo", prefixValue, TimeSpan.FromSeconds(120));

            SetCacheItem("1", "1", prefixKey);
            SetCacheItem("2", "2", prefixKey);
            SetCacheItem("3", "3", prefixKey);
            SetCacheItem("4", "4", prefixKey);
            SetCacheItem("4", "4", "xxx");

            await _provider.RemoveByPrefixAsync(prefixKey);

            GetCacheItem("1", prefixKey);
            GetCacheItem("2", prefixKey);
            GetCacheItem("3", prefixKey);
            GetCacheItem("4", prefixKey);

            var pre = _provider.Get<string>("xxx");
            var cacheKey = string.Concat(pre, "4");
            var val = _provider.Get<string>(cacheKey);
            Assert.True(val.HasValue);


            var afterPrefixValue = _provider.Get<string>(prefixKey);
            Assert.NotEqual(prefixValue, afterPrefixValue.Value);
        }

        private void SetCacheItem(string cacheKey, string cacheValue, string prefix)
        {
            var pre = _provider.Get<string>(prefix);

            cacheKey = string.Concat(pre, cacheKey);

            _provider.Set(cacheKey, cacheValue, _defaultTs);

            var val = _provider.Get<string>(cacheKey);
            Assert.Equal(cacheValue, val.Value);
        }


        private void GetCacheItem(string cacheKey, string prefix)
        {
            var pre = _provider.Get<string>(prefix);

            cacheKey = string.Concat(pre, cacheKey);

            var val = _provider.Get<string>(cacheKey);
            Assert.False(val.HasValue);
        }
    }
}
