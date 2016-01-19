﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using GoodAI.Core.Memory;
using Xunit;
using Xunit.Abstractions;

namespace CoreTests
{
    public class TensorDimensionsTests
    {
        private ITestOutputHelper m_output;

        public TensorDimensionsTests(ITestOutputHelper output)
        {
            m_output = output;
        }

        [Fact]
        public void ConstructsWithVariableNumberOfParams()
        {
            var dims = new TensorDimensionsV1(2, 3);

            Assert.Equal(2, dims.Count);
            Assert.Equal(2, dims[0]);
            Assert.Equal(3, dims[1]);
            Assert.False(dims.IsCustom);  // dimensions created in code are "default" and should not be saved to project
        }

        [Fact]
        public void CanBeComputedEvaluatesToTrue()
        {
            var dims = new TensorDimensionsV1(3, 4, -1, 5) { Size = 3*4*5*13 };

            Assert.True(dims.CanBeComputed);
            Assert.Equal(13, dims[2]);  // also check that the free dimension was correctly computed
        }

        [Fact]
        public void CanBeComputedEvaluatesToFalse()
        {
            var dims = new TensorDimensionsV1(3, 4, -1, 5) { Size = 37 };

            Assert.False(dims.CanBeComputed);
        }

        [Fact]
        public void EmptyDimensionsCanBeComputed()
        {
            var dims = new TensorDimensionsV1();
            Assert.False(dims.CanBeComputed);

            dims.Size = 4;
            Assert.True(dims.CanBeComputed);
            Assert.Equal(4, dims[0]);
        }

        [Fact]
        public void ComputedDimCanBeOne()
        {
            var dims = new TensorDimensionsV1(-1, 10) { Size = 10 };

            Assert.True(dims.CanBeComputed);
            Assert.Equal(1, dims[0]);
        }

        [Fact]
        public void DimensionsOfSizeOneAreAllowed()
        {
            var dims = new TensorDimensionsV1();

            dims.Set(new []{ 5, 1, 1 });
        }

        [Fact]
        public void ParseKeepsDimensionsOfSizeOne()
        {
            var dims = new TensorDimensionsV1();

            dims.Parse("1, 5, *, 1, 1");

            Assert.Equal(5, dims.Count);
            Assert.Equal(1, dims[0]);
            Assert.Equal(5, dims[1]);
            Assert.Equal(1, dims[4]);
            Assert.Equal("", dims.LastSetWarning);
        }

        [Fact]
        public void PrintIndicatesMismatchedDimsAndSize()
        {
            var dims = new TensorDimensionsV1(3, 3) { Size = 4 };

            Assert.Equal("3×3 (!)", dims.Print());
        }

        [Fact]
        public void DoesNotPrintTrailingOnes()
        {
            var dims = new TensorDimensionsV1(5, 1, 1) { Size = 5 };

            Assert.Equal("5", dims.Print(hideTrailingOnes: true));
        }

        [Fact]
        public void PrintsComputedTrailingOne()
        {
            var dims = new TensorDimensionsV1(4, 2, -1) { Size = 8 };

            Assert.Equal("4×2×1", dims.Print(hideTrailingOnes: true));
        }

        [Fact]
        public void PrintsOneOne()
        {
            var dims = new TensorDimensionsV1(1, 1);

            Assert.Equal("1 (!)", dims.Print(hideTrailingOnes: true));
        }

        [Fact]
        public void PrintsLeadingOrMiddleOnes()
        {
            var dims = new TensorDimensionsV1(1, 1, -1, 5, 1, 2, 1);

            Assert.Equal("1×1×?×5×1×2", dims.Print(hideTrailingOnes: true));
        }

        [Fact]
        public void ParseAutoAddsLeadingDim()
        {
            var dims = new TensorDimensionsV1();
            dims.Parse("2, 2, 2");

            Assert.Equal(4, dims.Count);
            Assert.Equal(-1, dims[0]);
            Assert.Equal(2, dims[1]);
        }

        [Fact]
        public void ParseDoesNotAutoAddDimWhenSizeMatches()
        {
            var dims = new TensorDimensionsV1() { Size = 2*2*2 };
            dims.Parse("2, 2, 2");

            Assert.Equal(3, dims.Count);
            Assert.Equal(2, dims[0]);
            Assert.Equal(2, dims[1]);
        }

        [Fact]
        public void DefaultDimIsRankOneOfSizeZero()
        {
            var defaultDims = new TensorDimensions();

            Assert.Equal(1, defaultDims.Rank);
            Assert.Equal(0, defaultDims[0]);
        }

        private TensorDimensions m_testDims = new TensorDimensions(5, 3, 2);

        [Fact]
        public void RankReturnsNumberOfDims()
        {
            Assert.Equal(3, m_testDims.Rank);
        }

        [Fact]
        public void PrintsEmptyDims()
        {
            var dims = new TensorDimensions();

            Assert.Equal("0", dims.Print());
        }

        [Fact]
        public void PrintsDims()
        {
            Assert.Equal("5×3×2", m_testDims.Print());
            Assert.Equal("5×3×2 [30]", m_testDims.Print(printTotalSize: true));
        }

        [Fact]
        public void ComputesCompatibleTensorDims()
        {
            var dims = TensorDimensions.GetBackwardCompatibleDims(10, 2);

            Assert.Equal(2, dims[0]);
            Assert.Equal(5, dims[1]);
        }

        [Fact]
        public void ComputesCompatibleTensorDimsWithWrongColumnHint()
        {
            var dims = TensorDimensions.GetBackwardCompatibleDims(10, 3);

            Assert.Equal(1, dims.Rank);
            Assert.Equal(10, dims[0]);
        }

        [Fact]
        public void ComputesCompatibleTensorDimsWithInvalidData()
        {
            var dims = TensorDimensions.GetBackwardCompatibleDims(0, 0);

            Assert.Equal(1, dims.Rank);
            Assert.Equal(0, dims[0]);
        }

        private static MyAbstractMemoryBlock GetMemBlock(TensorDimensions dims)
        {
            return new MyMemoryBlock<float> { Dims = dims };
        }

        [Fact]
        public void ColumnHintUsedWhenDivisible()
        {
            MyAbstractMemoryBlock memBlock = GetMemBlock(new TensorDimensions(12));
            memBlock.ColumnHint = 3;

            Assert.Equal(12, memBlock.Count);
            Assert.Equal(2, memBlock.Dims.Rank);
            Assert.Equal(4, memBlock.Dims[1]);
        }

        public class ColumnHintTestData
        {
            public ColumnHintTestData(TensorDimensions initialDims, int columnHint, TensorDimensions expectedDims, string comment)
            {
                InitialDims = initialDims;
                ColumnHint = columnHint;
                ExpectedDims = expectedDims;
                Comment = comment;
            }

            public TensorDimensions InitialDims { get; private set; }
            public int ColumnHint { get; private set; }
            public TensorDimensions ExpectedDims { get; private set; }
            public string Comment { get; private set; }
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public static readonly TheoryData ColumnHintData = new TheoryData<ColumnHintTestData>
        {
            new ColumnHintTestData(new TensorDimensions(12), 7, new TensorDimensions(12), "ColumnHint ignored when not divisible"),
            
            new ColumnHintTestData(new TensorDimensions(12), 3, new TensorDimensions(3, 4), "ColumnHint used when divisible"),
            
            new ColumnHintTestData(new TensorDimensions(6, 2), 3, new TensorDimensions(3, 4), "CH used for matrices while count remains constant"),
        };

        [Theory, MemberData("ColumnHintData")]
        public void ColumnHintTests(ColumnHintTestData testData)
        {
            m_output.WriteLine("Running '{0}'", testData.Comment);
            
            MyAbstractMemoryBlock memBlock = GetMemBlock(testData.InitialDims);
            memBlock.ColumnHint = testData.ColumnHint;

            Assert.Equal(testData.ExpectedDims.ElementCount, memBlock.Count);
            Assert.Equal(testData.ExpectedDims.Rank, memBlock.Dims.Rank);

            for (var i = 0; i < testData.ExpectedDims.Rank; i++)  // TODO: define Equals for TensorDimensions ?
                Assert.Equal(testData.ExpectedDims[i], memBlock.Dims[i]);
        }

        [Fact]
        public void UseColumnHintWhenSettingCountAfterIt()
        {
            MyAbstractMemoryBlock memBlock = GetMemBlock(new TensorDimensions());

            memBlock.ColumnHint = 3;
            Assert.Equal(0, memBlock.Dims.ElementCount);
            Assert.Equal(3, memBlock.ColumnHint);
           
            memBlock.Count = 12;
            Assert.Equal(12, memBlock.Dims.ElementCount);
            Assert.Equal(2, memBlock.Dims.Rank);
            Assert.Equal(4, memBlock.Dims[1]);
        }
    }
}
