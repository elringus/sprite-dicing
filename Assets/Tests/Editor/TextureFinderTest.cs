using System;
using System.Collections.Generic;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static SpriteDicing.Test.Helpers;

namespace SpriteDicing.Test
{
    public class TextureFinderTest
    {
        [Test]
        public void WhenNullFolderExceptionIsThrown ()
        {
            Throws<ArgumentException>(() => CollectAt(null));
        }

        [Test]
        public void WhenEmptyFolderExceptionIsThrown ()
        {
            Throws<ArgumentException>(() => CollectAt(""));
        }

        [Test]
        public void WhenFolderNotFoundExceptionIsThrown ()
        {
            Throws<ArgumentException>(() => CollectAt("C:\\Invalid"));
        }

        [Test]
        public void WhenNoTexturesFoundEmptyCollectionIsReturned ()
        {
            IsEmpty(CollectAt("Assets/Tests/Editor"));
        }

        [Test]
        public void WhenSubfoldersNotIncludedOnlyTopLevelTexturesAreReturned ()
        {
            AreEqual(Paths.TopLevel.Count, Collect(false).Count);
        }

        [Test]
        public void WhenSubfoldersIncludedAllTexturesAreReturned ()
        {
            AreEqual(Paths.All.Count, Collect(true).Count);
        }

        private static IReadOnlyList<string> CollectAt (string folderPath)
        {
            return new TextureFinder().FindAt(folderPath, false);
        }

        private static IReadOnlyList<string> Collect (bool includeSubfolders)
        {
            return new TextureFinder().FindAt(TextureFolderPath, includeSubfolders);
        }
    }
}
