// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2012 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Stride.Core;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Graphics.Data;

namespace Stride.Graphics
{
    /// <summary>
    /// All-in-One Buffer class linked <see cref="SharpDX.Direct3D11.Buffer"/>.
    /// </summary>
    /// <remarks>
    /// This class is able to create constant buffers, indexelementCountrtex buffers, structured buffer, raw buffers, argument buffers.
    /// </remarks>
    [DataSerializer(typeof(BufferSerializer))]
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<Buffer>), Profile = "Content")]
    [ContentSerializer(typeof(DataContentSerializer<Buffer>))]
    public partial class Buffer : GraphicsResource
    {
        protected int elementCount;
        private BufferDescription bufferDescription;

        public Buffer()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Buffer" /> class.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        protected Buffer(GraphicsDevice device) : base(device)
        {
        }

        /// <summary>
        /// Gets the description of this buffer.
        /// </summary>
        public BufferDescription Description
        {
            get { return bufferDescription; }
        }

        /// <summary>
        /// Value that identifies how the buffer is to be read from and written to.
        /// </summary>
        public GraphicsResourceUsage Usage
        {
            get { return bufferDescription.Usage; }
        }

        /// <summary>
        /// Buffer flags describing the type of buffer.
        /// </summary>
        public BufferFlags Flags
        {
            get { return bufferDescription.BufferFlags; }
        }

        /// <summary>
        /// Gets the size of the buffer in bytes.
        /// </summary>
        /// <value>
        /// The size of the buffer in bytes.
        /// </value>
        public int SizeInBytes
        {
            get { return bufferDescription.SizeInBytes; }
        }

        /// <summary>
        /// The size of the structure (in bytes) when it represents a structured/typed buffer.
        /// </summary>
        public int StructureByteStride
        {
            get { return bufferDescription.StructureByteStride; }
        }

        /// <summary>
        /// Gets the number of elements.
        /// </summary>
        /// <remarks>
        /// This value is valid for structured buffers, raw buffers and index buffers that are used as a SharedResourceView.
        /// </remarks>
        public int ElementCount
        {
            get
            {
                return elementCount;
            }
            protected set
            {
                elementCount = value;
            }
        }

        /// <summary>
        /// Gets the type of this buffer view.
        /// </summary>
        public BufferFlags ViewFlags { get; private set; }

        /// <summary>
        /// Gets the format of this buffer view.
        /// </summary>
        public PixelFormat ViewFormat { get; private set; }

        /// <summary>
        /// The initial Append/Consume buffer counter offset. A value of -1 indicates the current offset
        /// should be kept. Any other values set the hidden counter for that Appendable/Consumable
        /// Buffer. This value is only relevant for Buffers which have the 'Append' or 'Counter'
        /// flag, otherwise it is ignored. The value get's initialized to -1.
        /// </summary>
        public int InitialCounterOffset { get; set; } = -1;

        /// <summary>
        /// Return an equivalent staging texture CPU read-writable from this instance.
        /// </summary>
        /// <returns>A new instance of this buffer as a staging resource</returns>
        public Buffer ToStaging()
        {
            var stagingDesc = Description;
            stagingDesc.Usage = GraphicsResourceUsage.Staging;
            stagingDesc.BufferFlags = BufferFlags.None;
            return new Buffer(GraphicsDevice).InitializeFromImpl(stagingDesc, BufferFlags.None, ViewFormat, IntPtr.Zero);
        }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns>A clone of this instance</returns>
        /// <remarks>
        /// This method will not copy the content of the buffer to the clone
        /// </remarks>
        public Buffer Clone()
        {
            return new Buffer(GraphicsDevice).InitializeFromImpl(Description, ViewFlags, ViewFormat, IntPtr.Zero);
        }

        /// <summary>
        /// Gets the content of this buffer to an array of data.
        /// </summary>
        /// <typeparam name="TData">The type of the T data.</typeparam>
        /// <remarks>
        /// This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        /// This method creates internally a stagging resource if this texture is not already a stagging resouce, copies to it and map it to memory. Use method with explicit staging resource
        /// for optimal performances.</remarks>
        public TData[] GetData<TData>(CommandList commandList) where TData : unmanaged
        {
            var toData = new TData[this.Description.SizeInBytes / Unsafe.SizeOf<TData>()];
            GetData(commandList, toData);
            return toData;
        }

        /// <summary>
        /// Copies the content of this buffer to an array of data.
        /// </summary>
        /// <typeparam name="TData">The type of the T data.</typeparam>
        /// <param name="toData">The destination buffer to receive a copy of the texture datas.</param>
        /// <remarks>
        /// This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        /// This method creates internally a stagging resource if this texture is not already a stagging resouce, copies to it and map it to memory. Use method with explicit staging resource
        /// for optimal performances.</remarks>
        public void GetData<TData>(CommandList commandList, TData[] toData) where TData : unmanaged
        {
            // Get data from this resource
            if (this.Description.Usage == GraphicsResourceUsage.Staging)
            {
                // Directly if this is a staging resource
                GetData(commandList, this, toData);
            }
            else
            {
                // Unefficient way to use the Copy method using dynamic staging texture
                using var throughStaging = ToStaging();
                GetData(commandList, throughStaging, toData);
            }
        }

        /// <summary>
        /// Copies the content of this buffer to an array of data.
        /// </summary>
        /// <typeparam name="TData">The type of the T data.</typeparam>
        /// <param name="toData">The destination buffer to receive a copy of the texture datas.</param>
        /// <remarks>
        /// This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        /// This method creates internally a stagging resource if this texture is not already a stagging resouce, copies to it and map it to memory. Use method with explicit staging resource
        /// for optimal performances.</remarks>
        public void GetData<TData>(CommandList commandList, ref TData toData) where TData : unmanaged
        {
            // Get data from this resource
            if (this.Description.Usage == GraphicsResourceUsage.Staging)
            {
                // Directly if this is a staging resource
                GetData(commandList, this, ref toData);
            }
            else
            {
                // Unefficient way to use the Copy method using dynamic staging texture
                using var throughStaging = ToStaging();
                GetData(commandList, throughStaging, ref toData);
            }
        }

        /// <summary>
        /// Copies the content of this buffer from GPU memory to an array of data on CPU memory using a specific staging resource.
        /// </summary>
        /// <typeparam name="TData">The type of the T data.</typeparam>
        /// <param name="stagingTexture">The staging buffer used to transfer the buffer.</param>
        /// <param name="toData">To data.</param>
        /// <exception cref="System.ArgumentException">When strides is different from optimal strides, and TData is not the same size as the pixel format, or Width * Height != toData.Length</exception>
        /// <remarks>
        /// This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        /// </remarks>
        public unsafe void GetData<TData>(CommandList commandList, Buffer stagingTexture, ref TData toData) where TData : unmanaged
        {
            GetData(commandList, stagingTexture, MemoryMarshal.CreateSpan(ref toData, 1));
        }

        /// <summary>
        /// Copies the content of this buffer from GPU memory to an array of data on CPU memory using a specific staging resource.
        /// </summary>
        /// <typeparam name="TData">The type of the T data.</typeparam>
        /// <param name="stagingTexture">The staging buffer used to transfer the buffer.</param>
        /// <param name="toData">To data.</param>
        /// <exception cref="System.ArgumentException">When strides is different from optimal strides, and TData is not the same size as the pixel format, or Width * Height != toData.Length</exception>
        /// <remarks>
        /// This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        /// </remarks>
        public unsafe void GetData<TData>(CommandList commandList, Buffer stagingTexture, TData[] toData) where TData : unmanaged
        {
            GetData(commandList, stagingTexture, toData.AsSpan());
        }

        /// <summary>
        /// Copies the content an array of data on CPU memory to this buffer into GPU memory.
        /// </summary>
        /// <typeparam name="TData">The type of the T data.</typeparam>
        /// <param name="commandList">The <see cref="CommandList"/>.</param>
        /// <param name="fromData">The data to copy from.</param>
        /// <param name="offsetInBytes">The offset in bytes to write to.</param>
        /// <exception cref="System.ArgumentException"></exception>
        /// <remarks>
        /// See the unmanaged documentation about Map/UnMap for usage and restrictions.
        /// </remarks>
        public void SetData<TData>(CommandList commandList, ref TData fromData, int offsetInBytes = 0) where TData : unmanaged
        {
            SetData(commandList, MemoryMarshal.CreateReadOnlySpan(ref fromData, 1), offsetInBytes);
        }

        /// <summary>
        /// Copies the content an array of data on CPU memory to this buffer into GPU memory.
        /// </summary>
        /// <typeparam name="TData">The type of the T data.</typeparam>
        /// <param name="commandList">The <see cref="CommandList"/>.</param>
        /// <param name="fromData">The data to copy from.</param>
        /// <param name="offsetInBytes">The offset in bytes to write to.</param>
        /// <exception cref="System.ArgumentException"></exception>
        /// <remarks>
        /// See the unmanaged documentation about Map/UnMap for usage and restrictions.
        /// </remarks>
        public void SetData<TData>(CommandList commandList, TData[] fromData, int offsetInBytes = 0) where TData : unmanaged
        {
            SetData(commandList, (ReadOnlySpan<TData>)fromData.AsSpan(), offsetInBytes);
        }

        /// <summary>
        /// Copies the content of this buffer from GPU memory to a CPU memory using a specific staging resource.
        /// </summary>
        /// <param name="stagingTexture">The staging buffer used to transfer the buffer.</param>
        /// <param name="toData">To data pointer.</param>
        /// <exception cref="System.ArgumentException">When strides is different from optimal strides, and TData is not the same size as the pixel format, or Width * Height != toData.Length</exception>
        /// <remarks>
        /// This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        /// </remarks>
        [Obsolete("Use span instead")]
        public unsafe void GetData(CommandList commandList, Buffer stagingTexture, DataPointer toData)
        {
            GetData(commandList, stagingTexture, new Span<byte>((void*)toData.Pointer, toData.Size));
        }

        /// <summary>
        /// Copies the content of this buffer from GPU memory to a CPU memory using a specific staging resource.
        /// </summary>
        /// <param name="stagingTexture">The staging buffer used to transfer the buffer.</param>
        /// <param name="toData">To data pointer.</param>
        /// <exception cref="System.ArgumentException">When strides is different from optimal strides, and TData is not the same size as the pixel format, or Width * Height != toData.Length</exception>
        /// <remarks>
        /// This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        /// </remarks>
        public unsafe void GetData<T>(CommandList commandList, Buffer stagingTexture, Span<T> toData) where T : unmanaged
        {
            int toDataInBytes = toData.Length * sizeof(T);
            
            // Check size validity of data to copy to
            if (toDataInBytes > this.Description.SizeInBytes)
                throw new ArgumentException($"Length of {nameof(toData)} is larger than size of buffer");

            // Copy the texture to a staging resource
            if (!ReferenceEquals(this, stagingTexture))
                commandList.Copy(this, stagingTexture);

            // Map the staging resource to a CPU accessible memory
            var mappedResource = commandList.MapSubresource(stagingTexture, 0, MapMode.Read);
            fixed (void* pointer = toData)
                Unsafe.CopyBlockUnaligned(pointer, (void*)mappedResource.DataBox.DataPointer, (uint)toDataInBytes);
            // Make sure that we unmap the resource in case of an exception
            commandList.UnmapSubresource(mappedResource);
        }

        /// <summary>
        /// Copies the content an array of data on CPU memory to this buffer into GPU memory.
        /// </summary>
        /// <param name="commandList">The <see cref="CommandList"/>.</param>
        /// <param name="fromData">A data pointer.</param>
        /// <param name="offsetInBytes">The offset in bytes to write to.</param>
        /// <exception cref="System.ArgumentException"></exception>
        /// <remarks>
        /// See the unmanaged documentation about Map/UnMap for usage and restrictions.
        /// </remarks>
        [Obsolete("Use span instead")]
        public unsafe void SetData(CommandList commandList, DataPointer fromData, int offsetInBytes = 0)
        {
            SetData(commandList, new ReadOnlySpan<byte>((void*)fromData.Pointer, fromData.Size), offsetInBytes);
        }

        /// <summary>
        /// Copies the content an array of data on CPU memory to this buffer into GPU memory.
        /// </summary>
        /// <param name="commandList">The <see cref="CommandList"/>.</param>
        /// <param name="fromData">A data pointer.</param>
        /// <param name="offsetInBytes">The offset in bytes to write to.</param>
        /// <exception cref="System.ArgumentException"></exception>
        /// <remarks>
        /// See the unmanaged documentation about Map/UnMap for usage and restrictions.
        /// </remarks>
        public unsafe void SetData<T>(CommandList commandList, ReadOnlySpan<T> fromData, int offsetInBytes = 0) where T : unmanaged
        {
            int sizeInBytes = fromData.Length * sizeof(T);
            // Check size validity of data to copy to
            if (sizeInBytes > this.Description.SizeInBytes)
                throw new ArgumentException("Size of data to upload larger than size of buffer");

            fixed (void* pointer = fromData)
            {
                // If this texture is declared as default usage, we can only use UpdateSubresource, which is not optimal but better than nothing
                if (this.Description.Usage == GraphicsResourceUsage.Default)
                {
                    // Setup the dest region inside the buffer
                    if ((this.Description.BufferFlags & BufferFlags.ConstantBuffer) != 0)
                    {
                        commandList.UpdateSubresource(this, 0, new DataBox((nint)pointer, 0, 0));
                    }
                    else
                    {
                        var destRegion = new ResourceRegion(offsetInBytes, 0, 0, offsetInBytes + sizeInBytes, 1, 1);
                        commandList.UpdateSubresource(this, 0, new DataBox((nint)pointer, 0, 0), destRegion);
                    }
                }
                else
                {
                    if (offsetInBytes > 0)
                        throw new ArgumentException("offset is only supported for textured declared with ResourceUsage.Default", "offsetInBytes");

                    var mappedResource = commandList.MapSubresource(this, 0, Usage == GraphicsResourceUsage.Staging ? MapMode.Write : MapMode.WriteDiscard);
                    Unsafe.CopyBlockUnaligned((void*)mappedResource.DataBox.DataPointer, pointer, (uint)sizeInBytes);
                    commandList.UnmapSubresource(mappedResource);
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="Buffer" /> instance.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="description">The description of the buffer.</param>
        /// <param name="viewFormat">View format used if the buffer is used as a shared resource view.</param>
        /// <returns>An instance of a new <see cref="Buffer" /></returns>
        public static Buffer New(GraphicsDevice device, BufferDescription description, PixelFormat viewFormat = PixelFormat.None)
        {
            var bufferType = description.BufferFlags;
            return new Buffer(device).InitializeFromImpl(description, bufferType, viewFormat, IntPtr.Zero);
        }

        /// <summary>
        /// Creates a new <see cref="Buffer" /> instance.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="bufferSize">Size of the buffer in bytes.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of buffer.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>An instance of a new <see cref="Buffer" /></returns>
        public static Buffer New(GraphicsDevice device, int bufferSize, BufferFlags bufferFlags, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
        {
            return New(device, bufferSize, 0, bufferFlags, PixelFormat.None, usage);
        }

        /// <summary>
        /// Creates a new <see cref="Buffer" /> instance.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="elementCount">Number of T elment in this buffer.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of buffer.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>An instance of a new <see cref="Buffer" /></returns>
        public static Buffer<T> New<T>(GraphicsDevice device, int elementCount, BufferFlags bufferFlags, GraphicsResourceUsage usage = GraphicsResourceUsage.Default) where T : unmanaged
        {
            int bufferSize = Unsafe.SizeOf<T>() * elementCount;
            int elementSize = Unsafe.SizeOf<T>();

            var description = NewDescription(bufferSize, elementSize, bufferFlags, usage);
            return new Buffer<T>(device, description, bufferFlags, PixelFormat.None, IntPtr.Zero);
        }

        /// <summary>
        /// Creates a new <see cref="Buffer" /> instance.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="bufferSize">Size of the buffer in bytes.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of buffer.</param>
        /// <param name="viewFormat">The view format must be specified if the buffer is declared as a shared resource view.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>An instance of a new <see cref="Buffer" /></returns>
        public static Buffer New(GraphicsDevice device, int bufferSize, BufferFlags bufferFlags, PixelFormat viewFormat, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
        {
            return New(device, bufferSize, 0, bufferFlags, viewFormat, usage);
        }

        /// <summary>
        /// Creates a new <see cref="Buffer" /> instance.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="bufferSize">Size of the buffer in bytes.</param>
        /// <param name="elementSize">Size of an element in the buffer.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of buffer.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>An instance of a new <see cref="Buffer" /></returns>
        public static Buffer New(GraphicsDevice device, int bufferSize, int elementSize, BufferFlags bufferFlags, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
        {
            return New(device, bufferSize, elementSize, bufferFlags, PixelFormat.None, usage);
        }

        /// <summary>
        /// Creates a new <see cref="Buffer" /> instance.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="bufferSize">Size of the buffer in bytes.</param>
        /// <param name="elementSize">Size of an element in the buffer.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of buffer.</param>
        /// <param name="viewFormat">The view format must be specified if the buffer is declared as a shared resource view.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>An instance of a new <see cref="Buffer" /></returns>
        public static Buffer New(GraphicsDevice device, int bufferSize, int elementSize, BufferFlags bufferFlags, PixelFormat viewFormat, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
        {
            viewFormat = CheckPixelFormat(bufferFlags, elementSize, viewFormat);
            var description = NewDescription(bufferSize, elementSize, bufferFlags, usage);
            return new Buffer(device).InitializeFromImpl(description, bufferFlags, viewFormat, IntPtr.Zero);
        }

        /// <summary>
        /// Creates a new <see cref="Buffer" /> instance.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <typeparam name="T">Type of the buffer, to get the sizeof from.</typeparam>
        /// <param name="value">The initial value of this buffer.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of buffer.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>An instance of a new <see cref="Buffer" /></returns>
        public static Buffer<T> New<T>(GraphicsDevice device, ref T value, BufferFlags bufferFlags, GraphicsResourceUsage usage = GraphicsResourceUsage.Default) where T : unmanaged
        {
            return New(device, ref value, bufferFlags, PixelFormat.None, usage);
        }

        /// <summary>
        /// Creates a new <see cref="Buffer" /> instance.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <typeparam name="T">Type of the buffer, to get the sizeof from.</typeparam>
        /// <param name="value">The initial value of this buffer.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of buffer.</param>
        /// <param name="viewFormat">The view format must be specified if the buffer is declared as a shared resource view.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>An instance of a new <see cref="Buffer" /></returns>
        public static unsafe Buffer<T> New<T>(GraphicsDevice device, ref T value, BufferFlags bufferFlags, PixelFormat viewFormat, GraphicsResourceUsage usage = GraphicsResourceUsage.Default) where T : unmanaged
        {
            int bufferSize = Unsafe.SizeOf<T>();
            int elementSize = ((bufferFlags & BufferFlags.StructuredBuffer) != 0) ? Unsafe.SizeOf<T>() : 0;

            viewFormat = CheckPixelFormat(bufferFlags, elementSize, viewFormat);

            var description = NewDescription(bufferSize, elementSize, bufferFlags, usage);
            fixed (T* v = &value)
                return new Buffer<T>(device, description, bufferFlags, viewFormat, (nint)v);
        }

        /// <summary>
        /// Creates a new <see cref="Buffer" /> instance.
        /// </summary>
        /// <typeparam name="T">Type of the buffer, to get the sizeof from.</typeparam>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="initialValue">The initial value of this buffer.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of buffer.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>An instance of a new <see cref="Buffer" /></returns>
        public static Buffer<T> New<T>(GraphicsDevice device, T[] initialValue, BufferFlags bufferFlags, GraphicsResourceUsage usage = GraphicsResourceUsage.Default) where T : unmanaged
        {
            return New(device, (ReadOnlySpan<T>)initialValue.AsSpan(), bufferFlags, PixelFormat.None, usage);
        }

        /// <summary>
        /// Creates a new <see cref="Buffer" /> instance.
        /// </summary>
        /// <typeparam name="T">Type of the buffer, to get the sizeof from.</typeparam>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="initialValue">The initial value of this buffer.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of buffer.</param>
        /// <param name="viewFormat">The view format must be specified if the buffer is declared as a shared resource view.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>An instance of a new <see cref="Buffer" /></returns>
        public static Buffer<T> New<T>(GraphicsDevice device, T[] initialValue, BufferFlags bufferFlags, PixelFormat viewFormat, GraphicsResourceUsage usage = GraphicsResourceUsage.Default) where T : unmanaged
        {
            return New(device, (ReadOnlySpan<T>)initialValue.AsSpan(), bufferFlags, PixelFormat.None, usage);
        }

        /// <summary>
        /// Creates a new <see cref="Buffer" /> instance.
        /// </summary>
        /// <typeparam name="T">Type of the buffer, to get the sizeof from.</typeparam>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="initialValues">The initial data this buffer will contain.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of buffer.</param>
        /// <param name="viewFormat">The view format must be specified if the buffer is declared as a shared resource view.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>An instance of a new <see cref="Buffer" /></returns>
        public static unsafe Buffer<T> New<T>(GraphicsDevice device, ReadOnlySpan<T> initialValues, BufferFlags bufferFlags, PixelFormat viewFormat = PixelFormat.None, GraphicsResourceUsage usage = GraphicsResourceUsage.Default) where T : unmanaged
        {
            int bufferSize = sizeof(T) * initialValues.Length;
            int elementSize = sizeof(T);
            viewFormat = CheckPixelFormat(bufferFlags, elementSize, viewFormat);

            var description = NewDescription(bufferSize, elementSize, bufferFlags, usage);
            fixed (void* pointer = initialValues)
                return new Buffer<T>(device, description, bufferFlags, viewFormat, (nint)pointer);
        }

        /// <summary>
        /// Creates a new <see cref="Buffer" /> instance from a byte array.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="initialValue">The initial value of this buffer.</param>
        /// <param name="elementSize">Size of an element. Must be equal to 2 or 4 for an index buffer, or to the size of a struct for a structured/typed buffer. Can be set to 0 for other buffers.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of buffer.</param>
        /// <param name="viewFormat">The view format must be specified if the buffer is declared as a shared resource view.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>An instance of a new <see cref="Buffer" /></returns>
        public static Buffer New(GraphicsDevice device, ReadOnlySpan<byte> initialValue, int elementSize, BufferFlags bufferFlags, PixelFormat viewFormat = PixelFormat.None, GraphicsResourceUsage usage = GraphicsResourceUsage.Immutable)
        {
            return new Buffer(device).InitializeFrom(initialValue, elementSize, bufferFlags, viewFormat, usage);
        }

        /// <summary>
        /// Creates a new <see cref="Buffer" /> instance.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="dataPointer">The data pointer.</param>
        /// <param name="elementSize">Size of the element.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of buffer.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>An instance of a new <see cref="Buffer" /></returns>
        [Obsolete("Use span instead")]
        public static Buffer New(GraphicsDevice device, DataPointer dataPointer, int elementSize, BufferFlags bufferFlags, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
        {
            return New(device, dataPointer, elementSize, bufferFlags, PixelFormat.None, usage);
        }

        /// <summary>
        /// Creates a new <see cref="Buffer" /> instance.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="dataPointer">The data pointer.</param>
        /// <param name="elementSize">Size of the element.</param>
        /// <param name="bufferFlags">The buffer flags to specify the type of buffer.</param>
        /// <param name="viewFormat">The view format must be specified if the buffer is declared as a shared resource view.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>An instance of a new <see cref="Buffer" /></returns>
        [Obsolete("Use span instead")]
        public static Buffer New(GraphicsDevice device, DataPointer dataPointer, int elementSize, BufferFlags bufferFlags, PixelFormat viewFormat, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
        {
            int bufferSize = dataPointer.Size;
            viewFormat = CheckPixelFormat(bufferFlags, elementSize, viewFormat);
            var description = NewDescription(bufferSize, elementSize, bufferFlags, usage);
            return new Buffer(device).InitializeFromImpl(description, bufferFlags, viewFormat, dataPointer.Pointer);
        }

        internal unsafe Buffer InitializeFrom(ReadOnlySpan<byte> initialValue, int elementSize, BufferFlags bufferFlags, PixelFormat viewFormat = PixelFormat.None, GraphicsResourceUsage usage = GraphicsResourceUsage.Immutable)
        {
            int bufferSize = initialValue.Length;
            viewFormat = CheckPixelFormat(bufferFlags, elementSize, viewFormat);

            var description = NewDescription(bufferSize, elementSize, bufferFlags, usage);
            fixed (void* initial = initialValue)
                return InitializeFromImpl(description, bufferFlags, viewFormat, (nint)initial);
        }

        private static PixelFormat CheckPixelFormat(BufferFlags bufferFlags, int elementSize, PixelFormat viewFormat)
        {
            if ((bufferFlags & BufferFlags.IndexBuffer) != 0 && (bufferFlags & BufferFlags.ShaderResource) != 0)
            {
                if (elementSize != 2 && elementSize != 4)
                    throw new ArgumentException("Element size must be set to sizeof(short) = 2 or sizeof(int) = 4 for index buffer if index buffer is bound to a ShaderResource", "elementSize");

                viewFormat = elementSize == 2 ? PixelFormat.R16_UInt : PixelFormat.R32_UInt;
            }
            return viewFormat;
        }

        private static BufferDescription NewDescription(int bufferSize, int elementSize, BufferFlags bufferFlags, GraphicsResourceUsage usage)
        {
            return new BufferDescription()
            {
                SizeInBytes = bufferSize,
                StructureByteStride = (bufferFlags & BufferFlags.StructuredBuffer) != 0 ? elementSize : 0,
                BufferFlags = bufferFlags,
                Usage = usage,
            };
        }

        /// <summary>
        /// Reload <see cref="Buffer"/> from given data if <see cref="GraphicsDevice"/> has been reset.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataPointer">The data pointer.</param>
        /// <returns>This instance.</returns>
        public Buffer RecreateWith<T>(T[] dataPointer) where T : unmanaged
        {
            Reload = (graphicsResource, services) => ((Buffer)graphicsResource).Recreate(dataPointer);

            return this;
        }

        /// <summary>
        /// Reload <see cref="Buffer"/> from given data if <see cref="GraphicsDevice"/> has been reset.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataPointer">The data pointer.</param>
        /// <returns>This instance.</returns>
        public Buffer RecreateWith(IntPtr dataPointer)
        {
            Reload = (graphicsResource, services) => ((Buffer)graphicsResource).Recreate(dataPointer);

            return this;
        }

        /// <summary>
        /// Explicitly recreate buffer with given data. Usually called after a <see cref="GraphicsDevice"/> reset.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataPointer"></param>
        public unsafe void Recreate<T>(T[] dataPointer) where T : unmanaged
        {
            fixed (void* data = dataPointer)
                Recreate((nint)data);
        }
    }

    /// <summary>
    /// A buffer with typed information.
    /// </summary>
    /// <typeparam name="T">Type of an element of this buffer.</typeparam>
    public class Buffer<T> : Buffer where T : unmanaged
    {
        protected internal Buffer(GraphicsDevice device, BufferDescription description, BufferFlags viewFlags, PixelFormat viewFormat, IntPtr dataPointer) : base(device)
        {
            InitializeFromImpl(description, viewFlags, viewFormat, dataPointer);
            this.ElementSize = Unsafe.SizeOf<T>();
            this.ElementCount = Description.SizeInBytes / ElementSize;
        }

        /// <summary>
        /// Gets the size of element T.
        /// </summary>
        public readonly int ElementSize;

        /// <summary>
        /// Gets the content of this texture to an array of data.
        /// </summary>
        /// <returns>An array of data.</returns>
        /// <remarks>This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice" />.
        /// This method creates internally a stagging resource if this texture is not already a stagging resouce, copies to it and map it to memory. Use method with explicit staging resource
        /// for optimal performances.</remarks>
        public T[] GetData(CommandList commandList)
        {
            return GetData<T>(commandList);
        }

        /// <summary>
        /// Copies the content of a single structure data from CPU memory to this buffer into GPU memory.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="fromData">The data to copy from.</param>
        /// <param name="offsetInBytes">The offset in bytes to write to.</param>
        /// <exception cref="System.ArgumentException"></exception>
        /// <remarks>
        /// This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>. See the unmanaged documentation about Map/UnMap for usage and restrictions.
        /// </remarks>
        public void SetData(CommandList commandList, ref T fromData, int offsetInBytes = 0)
        {
            base.SetData(commandList, ref fromData, offsetInBytes);
        }

        /// <summary>
        /// Copies the content an array of data from CPU memory to this buffer into GPU memory.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="fromData">The data to copy from.</param>
        /// <param name="offsetInBytes">The offset in bytes to write to.</param>
        /// <remarks>
        /// This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>. See the unmanaged documentation about Map/UnMap for usage and restrictions.
        /// </remarks>
        public void SetData(CommandList commandList, T[] fromData, int offsetInBytes = 0)
        {
            base.SetData(commandList, fromData, offsetInBytes);
        }
    }
}
