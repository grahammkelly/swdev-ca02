using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LMS
{
    public class NotFoundException : Exception {
        public NotFoundException(string message) : base(message) {}
    }

    public class AlreadyExistsException : Exception {
        public AlreadyExistsException(string message) : base(message) {}
    }
}