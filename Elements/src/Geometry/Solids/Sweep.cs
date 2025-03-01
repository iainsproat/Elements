namespace Elements.Geometry.Solids
{
    /// <summary>A sweep of a profile along a curve.</summary>
    public partial class Sweep : SolidOperation, System.ComponentModel.INotifyPropertyChanged
    {
        private Profile _profile;
        private Curve _curve;
        private double _startSetback;
        private double _endSetback;
        private double _profileRotation;

        /// <summary>
        /// Construct a sweep.
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="curve"></param>
        /// <param name="startSetback"></param>
        /// <param name="endSetback"></param>
        /// <param name="profileRotation"></param>
        /// <param name="isVoid"></param>
        [Newtonsoft.Json.JsonConstructor]
        public Sweep(Profile @profile, Curve @curve, double @startSetback, double @endSetback, double @profileRotation, bool @isVoid)
            : base(isVoid)
        {
            this._profile = @profile;
            this._curve = @curve;
            this._startSetback = @startSetback;
            this._endSetback = @endSetback;
            this._profileRotation = @profileRotation;

            this.PropertyChanged += (sender, args) => { UpdateGeometry(); };
            UpdateGeometry();
        }

        /// <summary>The id of the profile to be swept along the curve.</summary>
        [Newtonsoft.Json.JsonProperty("Profile", Required = Newtonsoft.Json.Required.AllowNull)]
        public Profile Profile
        {
            get { return _profile; }
            set
            {
                if (_profile != value)
                {
                    _profile = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>The curve along which the profile will be swept.</summary>
        [Newtonsoft.Json.JsonProperty("Curve", Required = Newtonsoft.Json.Required.AllowNull)]
        public Curve Curve
        {
            get { return _curve; }
            set
            {
                if (_curve != value)
                {
                    _curve = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>The amount to set back the resulting solid from the start of the curve.</summary>
        [Newtonsoft.Json.JsonProperty("StartSetback", Required = Newtonsoft.Json.Required.Always)]
        public double StartSetback
        {
            get { return _startSetback; }
            set
            {
                if (_startSetback != value)
                {
                    _startSetback = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>The amount to set back the resulting solid from the end of the curve.</summary>
        [Newtonsoft.Json.JsonProperty("EndSetback", Required = Newtonsoft.Json.Required.Always)]
        public double EndSetback
        {
            get { return _endSetback; }
            set
            {
                if (_endSetback != value)
                {
                    _endSetback = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>The rotation of the profile around the sweep's curve.</summary>
        [Newtonsoft.Json.JsonProperty("ProfileRotation", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public double ProfileRotation
        {
            get { return _profileRotation; }
            set
            {
                if (_profileRotation != value)
                {
                    _profileRotation = value;
                    RaisePropertyChanged();
                }
            }
        }

        private void UpdateGeometry()
        {
            var profileTrans = new Transform();
            profileTrans.Rotate(profileTrans.ZAxis, this.ProfileRotation);
            this._solid = Kernel.Instance.CreateSweepAlongCurve(profileTrans.OfProfile(this._profile), this._curve, this._startSetback, this._endSetback);
        }
    }
}