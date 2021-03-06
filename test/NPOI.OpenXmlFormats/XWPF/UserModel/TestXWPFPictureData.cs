/* ====================================================================
   Licensed to the Apache Software Foundation (ASF) under one or more
   contributor license agreements.  See the NOTICE file distributed with
   this work for Additional information regarding copyright ownership.
   The ASF licenses this file to You under the Apache License, Version 2.0
   (the "License"); you may not use this file except in compliance with
   the License.  You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
==================================================================== */

namespace NPOI.XWPF.UserModel
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using NPOI.OpenXml4Net.OPC;
    using NPOI.Util;
    using NPOI.XSSF.UserModel;
    using NPOI.XWPF;
    using NPOI.XWPF.Model;
    using System.Xml;

    [TestFixture]
    public class TestXWPFPictureData
    {
        [Test]
        public void TestRead()
        {
            XWPFDocument sampleDoc = XWPFTestDataSamples.OpenSampleDocument("VariousPictures.docx");
            IList<XWPFPictureData> pictures = sampleDoc.AllPictures;

            Assert.AreEqual(5, pictures.Count);
            String[] ext = { "wmf", "png", "emf", "emf", "jpeg" };
            for (int i = 0; i < pictures.Count; i++)
            {
                Assert.AreEqual(ext[i], pictures[(i)].SuggestFileExtension());
            }

            int num = pictures.Count;

            byte[] pictureData = XWPFTestDataSamples.GetImage("nature1.jpg");

            String relationId = sampleDoc.AddPictureData(pictureData, (int)PictureType.JPEG);
            // picture list was updated
            Assert.AreEqual(num + 1, pictures.Count);
            XWPFPictureData pict = (XWPFPictureData)sampleDoc.GetRelationById(relationId);
            Assert.AreEqual("jpeg", pict.SuggestFileExtension());
            Assert.IsTrue(Arrays.Equals(pictureData, pict.Data));
        }
        [Test]
        public void TestPictureInHeader()
        {
            XWPFDocument sampleDoc = XWPFTestDataSamples.OpenSampleDocument("headerPic.docx");
            verifyOneHeaderPicture(sampleDoc);

            XWPFDocument readBack = XWPFTestDataSamples.WriteOutAndReadBack(sampleDoc);
            verifyOneHeaderPicture(readBack);
        }

        private void verifyOneHeaderPicture(XWPFDocument sampleDoc)
        {
            XWPFHeaderFooterPolicy policy = sampleDoc.GetHeaderFooterPolicy();

            XWPFHeader header = policy.GetDefaultHeader();

            IList<XWPFPictureData> pictures = header.AllPictures;
            Assert.AreEqual(1, pictures.Count);
        }
        [Test]
        public void TestNew()
        {
            XWPFDocument doc = XWPFTestDataSamples.OpenSampleDocument("EmptyDocumentWithHeaderFooter.docx");
            byte[] jpegData = XWPFTestDataSamples.GetImage("nature1.jpg");
            Assert.IsNotNull(jpegData);
            byte[] gifData = XWPFTestDataSamples.GetImage("nature1.gif");
            Assert.IsNotNull(gifData);
            byte[] pngData = XWPFTestDataSamples.GetImage("nature1.png");
            Assert.IsNotNull(pngData);

            IList<XWPFPictureData> pictures = doc.AllPictures;
            Assert.AreEqual(0, pictures.Count);

            // Document shouldn't have any image relationships
            Assert.AreEqual(13, doc.GetPackagePart().Relationships.Size);
            foreach (PackageRelationship rel in doc.GetPackagePart().Relationships)
            {
                if (rel.RelationshipType.Equals(XSSFRelation.IMAGE_JPEG.Relation))
                {
                    Assert.Fail("Shouldn't have JPEG yet");
                }
            }

            // Add the image
            String relationId = doc.AddPictureData(jpegData, (int)PictureType.JPEG);
            Assert.AreEqual(1, pictures.Count);
            XWPFPictureData jpgPicData = (XWPFPictureData)doc.GetRelationById(relationId);
            Assert.AreEqual("jpeg", jpgPicData.SuggestFileExtension());
            Assert.IsTrue(Arrays.Equals(jpegData, jpgPicData.Data));

            // Ensure it now has one
            Assert.AreEqual(14, doc.GetPackagePart().Relationships.Size);
            PackageRelationship jpegRel = null;
            foreach (PackageRelationship rel in doc.GetPackagePart().Relationships)
            {
                if (rel.RelationshipType.Equals(XWPFRelation.IMAGE_JPEG.Relation))
                {
                    if (jpegRel != null)
                        Assert.Fail("Found 2 jpegs!");
                    jpegRel = rel;
                }
            }
            Assert.IsNotNull(jpegRel, "JPEG Relationship not found");

            // Check the details
            Assert.AreEqual(XWPFRelation.IMAGE_JPEG.Relation, jpegRel.RelationshipType);
            Assert.AreEqual("/word/document.xml", jpegRel.Source.PartName.ToString());
            Assert.AreEqual("/word/media/image1.jpeg", jpegRel.TargetUri.OriginalString);

            XWPFPictureData pictureDataByID = doc.GetPictureDataByID(jpegRel.Id);
            Assert.IsTrue(Arrays.Equals(jpegData, pictureDataByID.Data));

            // Save an re-load, check it appears
            doc = XWPFTestDataSamples.WriteOutAndReadBack(doc);
            Assert.AreEqual(1, doc.AllPictures.Count);
            Assert.AreEqual(1, doc.AllPackagePictures.Count);

            // verify the picture that we read back in
            pictureDataByID = doc.GetPictureDataByID(jpegRel.Id);
            Assert.IsTrue(Arrays.Equals(jpegData, pictureDataByID.Data));
        }      
    }
}