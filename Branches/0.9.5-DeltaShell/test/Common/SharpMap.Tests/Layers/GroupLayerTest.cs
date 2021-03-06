using System;
using System.ComponentModel;
using NUnit.Framework;
using Rhino.Mocks;
using SharpMap.Api;
using SharpMap.Layers;

namespace SharpMap.Tests.Layers
{
    [TestFixture]
    public class GroupLayerTest
    {
        [Test]
        public void EnablingChildLayerBubblesOnePropertyChangedEvent()
        {
            //this is needed to let the mapcontrol refresh see issue 2749
            int callCount = 0;
            var layerGroup = new GroupLayer();
            var childLayer = new VectorLayer();
            layerGroup.Layers.Add(childLayer);
            ((INotifyPropertyChanged)layerGroup).PropertyChanged += (sender, args) => callCount++;
            childLayer.Visible = false;
            
            Assert.AreEqual(1,callCount);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "It is not allowed to add or remove layers from a grouplayer that has a read-only layers collection")]
        public void MutatingAGroupLayerWithHasReadonlyLayerCollectionThrows()
        {
            var layerGroup = new GroupLayer {HasReadOnlyLayersCollection = true};
            var childLayer = new VectorLayer();
            layerGroup.Layers.Add(childLayer);
            
        }

        [Test]
        public void MutatingANestedGroupLayerWhenParentHasReadonlyLayerCollectionDoesNotThrow()
        {
            var layerGroup = new GroupLayer();
            var childGroupLayer = new GroupLayer();
            var childLayer = new VectorLayer();
            layerGroup.Layers.Add(childGroupLayer);

            layerGroup.HasReadOnlyLayersCollection = true;

            childGroupLayer.Layers.Add(childLayer); //no exception!

            Assert.AreEqual(1, childGroupLayer.Layers.Count);
        }

        [Test]
        public void GroupLayerCloneTest()
        {
            var mocks = new MockRepository();
            var map = mocks.StrictMock<Map>();
            map.Expect(m => m.IsDisposing).Return(false).Repeat.Any();

            var layer1 = mocks.DynamicMock<ILayer>();
            var layer1Clone = mocks.DynamicMock<ILayer>();
            layer1.Expect(l => l.Clone()).Return(layer1Clone).Repeat.Once();

            mocks.ReplayAll();

            var originalGroupLayer = new GroupLayer("original");
            originalGroupLayer.Map = map;
            originalGroupLayer.Layers.AddRange(new[]{layer1});
            originalGroupLayer.HasReadOnlyLayersCollection = true;

            var clone = (GroupLayer) originalGroupLayer.Clone();

            Assert.AreEqual("original", clone.Name);
            Assert.IsNull(clone.Map);
            Assert.IsTrue(clone.HasReadOnlyLayersCollection);

            mocks.VerifyAll();
        }
    }
}