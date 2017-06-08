﻿using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;


namespace PSDUnity
{
    public interface IImageImport
    {
        UINode DrawImage(Image image, UINode parent);
    }
}
