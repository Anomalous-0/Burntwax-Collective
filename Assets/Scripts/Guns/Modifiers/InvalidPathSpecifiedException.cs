using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InvalidPathSpecifiedException : Exception {
    public InvalidPathSpecifiedException(string Attribute): base($"{Attribute} does not exist at the provided path") {
        
    }
}
