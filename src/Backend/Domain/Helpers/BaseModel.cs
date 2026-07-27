using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace gtas_vpp_be.Model.Helpers
{
    public abstract class BaseModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public virtual Guid Id { get; set; }

        [StringLength(500)]
        public virtual string? Description { get; set; }

        public virtual int CreatedByUserId { get; set; }

        public virtual DateTime CreatedAtUtc { get; set; }

        public virtual int UpdatedByUserId { get; set; }

        public virtual DateTime UpdatedAtUtc { get; set; }

        [DefaultValue(false)]
        public virtual bool IsDeleted { get; set; }
    }
}
