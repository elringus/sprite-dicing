using System;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static SpriteDicing.Test.Helpers;
using static SpriteDicing.TextureFinder;

namespace SpriteDicing.Test
{
    public class TextureFinderTest
    {
        [Test]
        public void WhenNullFolderExceptionIsThrown ()
        {
            Throws<ArgumentException>(() => FindAt(null, false));
        }

        [Test]
        public void WhenEmptyFolderExceptionIsThrown ()
        {
            Throws<ArgumentException>(() => FindAt("", false));
        }

        [Test]
        public void WhenFolderNotFoundExceptionIsThrown ()
        {
            Throws<ArgumentException>(() => FindAt("C:\\Invalid", false));
        }

        [Test]
        public void WhenNoTexturesFoundEmptyCollectionIsReturned ()
        {
            IsEmpty(FindAt("Assets/Tests/Editor", true));
        }

        [Test]
        public void WhenSubfoldersNotIncludedOnlyTopLevelTexturesAreReturned ()
        {
            AreEqual(Paths.TopLevel.Count, FindAt(TextureFolderPath, false).Count);
        }

        [Test]
        public void WhenSubfoldersIncludedAllTexturesAreReturned ()
        {
            AreEqual(Paths.All.Count, FindAt(TextureFolderPath, true).Count);
        }
    }
}
