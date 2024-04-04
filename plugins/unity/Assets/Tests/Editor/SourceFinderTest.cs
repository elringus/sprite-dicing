using System;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static SpriteDicing.Test.Helpers;
using static SpriteDicing.SourceFinder;

namespace SpriteDicing.Test
{
    public class SourceFinderTest
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

        [Test]
        public void PathsAreOrderedAlphanumerically ()
        {
            var paths = FindAt(TextureFolderPath, false);
            AreEqual(Paths.RGB1x3, paths[0]);
            AreEqual(Paths.RGB3x1, paths[1]);
            AreEqual(Paths.RGB4x4, paths[2]);
        }
    }
}
