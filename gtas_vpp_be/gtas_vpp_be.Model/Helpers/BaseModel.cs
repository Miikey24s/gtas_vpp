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

        public virtual int CreateUserId { get; set; }

        public virtual DateTime CreateDate { get; set; }

        public virtual int UpdateUserId { get; set; }

        public virtual DateTime UpdateDate { get; set; }

        [DefaultValue(false)]
        public virtual bool IsDeleted { get; set; }
    }
}
