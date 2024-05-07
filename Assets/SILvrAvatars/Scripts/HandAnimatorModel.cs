using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Normal.Realtime;
using Normal.Realtime.Serialization;

namespace SILvr.Avatars
{
    [RealtimeModel]
    public partial class HandAnimatorModel
    {
        [RealtimeProperty(1, false)]
        private float _thumb;
        [RealtimeProperty(2, false)]
        private float _pointer;
        [RealtimeProperty(3, false)]
        private float _middle;
    }
}


/* ----- Begin Normal Autogenerated Code ----- */
namespace SILvr.Avatars {
    public partial class HandAnimatorModel : RealtimeModel {
        public float thumb {
            get {
                return _thumbProperty.value;
            }
            set {
                if (_thumbProperty.value == value) return;
                _thumbProperty.value = value;
                InvalidateUnreliableLength();
            }
        }
        
        public float pointer {
            get {
                return _pointerProperty.value;
            }
            set {
                if (_pointerProperty.value == value) return;
                _pointerProperty.value = value;
                InvalidateUnreliableLength();
            }
        }
        
        public float middle {
            get {
                return _middleProperty.value;
            }
            set {
                if (_middleProperty.value == value) return;
                _middleProperty.value = value;
                InvalidateUnreliableLength();
            }
        }
        
        public enum PropertyID : uint {
            Thumb = 1,
            Pointer = 2,
            Middle = 3,
        }
        
        #region Properties
        
        private UnreliableProperty<float> _thumbProperty;
        
        private UnreliableProperty<float> _pointerProperty;
        
        private UnreliableProperty<float> _middleProperty;
        
        #endregion
        
        public HandAnimatorModel() : base(null) {
            _thumbProperty = new UnreliableProperty<float>(1, _thumb);
            _pointerProperty = new UnreliableProperty<float>(2, _pointer);
            _middleProperty = new UnreliableProperty<float>(3, _middle);
        }
        
        protected override int WriteLength(StreamContext context) {
            var length = 0;
            length += _thumbProperty.WriteLength(context);
            length += _pointerProperty.WriteLength(context);
            length += _middleProperty.WriteLength(context);
            return length;
        }
        
        protected override void Write(WriteStream stream, StreamContext context) {
            var writes = false;
            writes |= _thumbProperty.Write(stream, context);
            writes |= _pointerProperty.Write(stream, context);
            writes |= _middleProperty.Write(stream, context);
            if (writes) InvalidateContextLength(context);
        }
        
        protected override void Read(ReadStream stream, StreamContext context) {
            var anyPropertiesChanged = false;
            while (stream.ReadNextPropertyID(out uint propertyID)) {
                var changed = false;
                switch (propertyID) {
                    case (uint) PropertyID.Thumb: {
                        changed = _thumbProperty.Read(stream, context);
                        break;
                    }
                    case (uint) PropertyID.Pointer: {
                        changed = _pointerProperty.Read(stream, context);
                        break;
                    }
                    case (uint) PropertyID.Middle: {
                        changed = _middleProperty.Read(stream, context);
                        break;
                    }
                    default: {
                        stream.SkipProperty();
                        break;
                    }
                }
                anyPropertiesChanged |= changed;
            }
            if (anyPropertiesChanged) {
                UpdateBackingFields();
            }
        }
        
        private void UpdateBackingFields() {
            _thumb = thumb;
            _pointer = pointer;
            _middle = middle;
        }
        
    }
}
/* ----- End Normal Autogenerated Code ----- */
